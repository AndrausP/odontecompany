using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Scheduling.Domain.Enums;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IReceitaPorProcedimentoProvider"/> — agrupa agendamentos
/// CONCLUÍDOS por `ProcedimentoId`, soma `ValorConsulta`. Nome do procedimento resolvido num
/// segundo passo (não dá pra fazer `GROUP BY` com join direto de forma limpa em EF) — mesmo
/// padrão de "agrupa primeiro, resolve nome depois" que `AgendaSummaryProvider` já usa pra status.
/// </summary>
public sealed class ReceitaPorProcedimentoProvider : IReceitaPorProcedimentoProvider
{
    private readonly SchedulingDbContext _context;

    public ReceitaPorProcedimentoProvider(SchedulingDbContext context) => _context = context;

    public async Task<IReadOnlyList<ReceitaProcedimentoDto>> ObterAsync(Guid organizationId, DateTime dataInicio, DateTime dataFim, CancellationToken ct = default)
    {
        var agrupado = await _context.Agendamentos
            .AsNoTracking()
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de PatientRepository.CpfExistsAsync
            .Where(a => a.OrganizationId == organizationId
                && a.Status == AgendamentoStatus.Concluido
                && a.Periodo.Inicio >= dataInicio
                && a.Periodo.Inicio <= dataFim)
            .GroupBy(a => a.ProcedimentoId)
            .Select(g => new { ProcedimentoId = g.Key, Quantidade = g.Count(), ValorTotal = g.Sum(a => a.ValorConsulta ?? 0) })
            .ToListAsync(ct);

        var procedimentoIds = agrupado.Where(g => g.ProcedimentoId is not null).Select(g => g.ProcedimentoId!.Value).ToList();
        var nomes = await _context.Procedimentos
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => procedimentoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nome, ct);

        return agrupado
            .Select(g => new ReceitaProcedimentoDto(
                ProcedimentoId: g.ProcedimentoId,
                ProcedimentoNome: g.ProcedimentoId is Guid id ? nomes.GetValueOrDefault(id, "Procedimento removido") : "Sem procedimento",
                Quantidade: g.Quantidade,
                ValorTotal: g.ValorTotal))
            .OrderByDescending(r => r.ValorTotal)
            .ToList();
    }
}
