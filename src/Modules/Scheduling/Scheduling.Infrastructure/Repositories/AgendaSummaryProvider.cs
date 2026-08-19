using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Scheduling.Domain.Enums;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Repositories;

/// <summary>Implementação de <see cref="IAgendaSummaryProvider"/> — só `COUNT`/`GROUP BY` na própria tabela, escopado por OrganizationId explícito.</summary>
public sealed class AgendaSummaryProvider : IAgendaSummaryProvider
{
    private readonly SchedulingDbContext _context;

    public AgendaSummaryProvider(SchedulingDbContext context) => _context = context;

    public async Task<AgendaResumoDto> ObterResumoAsync(Guid organizationId, DateTime dataInicio, DateTime dataFim, CancellationToken ct = default)
    {
        var agendamentosNoPeriodo = _context.Agendamentos
            .AsNoTracking()
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de PatientRepository.CpfExistsAsync
            .Where(a => a.OrganizationId == organizationId && a.Periodo.Inicio >= dataInicio && a.Periodo.Inicio <= dataFim);

        var porStatus = await agendamentosNoPeriodo
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int ContarStatus(AgendamentoStatus status) => porStatus.FirstOrDefault(p => p.Status == status)?.Count ?? 0;

        return new AgendaResumoDto(
            TotalAgendamentos: porStatus.Sum(p => p.Count),
            Agendados: ContarStatus(AgendamentoStatus.Agendado),
            Confirmados: ContarStatus(AgendamentoStatus.Confirmado),
            Concluidos: ContarStatus(AgendamentoStatus.Concluido),
            Cancelados: ContarStatus(AgendamentoStatus.Cancelado));
    }
}
