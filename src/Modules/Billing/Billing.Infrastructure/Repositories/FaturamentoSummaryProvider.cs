using Billing.Contracts;
using Billing.Domain.Enums;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IFaturamentoSummaryProvider"/>. Faturas canceladas são excluídas
/// dos totais (não representam valor devido nem recebido) mas ainda contam pro período —
/// query direta na própria tabela, sem JOIN cruzado com outro módulo.
/// </summary>
public sealed class FaturamentoSummaryProvider : IFaturamentoSummaryProvider
{
    private readonly BillingDbContext _context;

    public FaturamentoSummaryProvider(BillingDbContext context) => _context = context;

    public async Task<FaturamentoResumoDto> ObterResumoAsync(Guid organizationId, DateTime dataInicio, DateTime dataFim, CancellationToken ct = default)
    {
        var faturasNoPeriodo = await _context.Faturas
            .AsNoTracking()
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de PatientRepository.CpfExistsAsync
            .Include(f => f.Parcelas)
            .Where(f => f.OrganizationId == organizationId && f.CreatedAt >= dataInicio && f.CreatedAt <= dataFim && f.Status != StatusFatura.Cancelada)
            .ToListAsync(ct);

        var valorTotalFaturado = faturasNoPeriodo.Sum(f => f.ValorTotal);
        var valorTotalRecebido = faturasNoPeriodo.Sum(f => f.Parcelas.Where(p => p.Status == StatusParcela.Paga).Sum(p => p.ValorParcela));

        return new FaturamentoResumoDto(
            ValorTotalFaturado: valorTotalFaturado,
            ValorTotalRecebido: valorTotalRecebido,
            ValorTotalPendente: valorTotalFaturado - valorTotalRecebido,
            QuantidadeFaturas: faturasNoPeriodo.Count);
    }
}
