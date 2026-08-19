using Billing.Contracts;
using Billing.Domain.Enums;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IComissaoSummaryProvider"/>. Regime de CAIXA: só soma parcela com
/// <c>Status == Paga</c> e <c>DataPagamento</c> dentro de [dataInicio, dataFim] — nunca o
/// <c>ValorTotal</c> da fatura inteira (essa é <c>Fatura.CalcularValorComissao</c>, regime de
/// competência, usado noutro fluxo). Comissão calculada POR PARCELA paga
/// (<c>Fatura.CalcularComissaoSobre(parcela.ValorParcela)</c>) e só depois somada — decisão D2 do
/// Tech Lead (docs/tasks/022): fatura com parcelamento parcial não pode pagar comissão cheia.
/// Faturas canceladas ficam de fora (R2 da task 022).
/// </summary>
public sealed class ComissaoSummaryProvider : IComissaoSummaryProvider
{
    private readonly BillingDbContext _context;

    public ComissaoSummaryProvider(BillingDbContext context) => _context = context;

    public async Task<IReadOnlyList<ComissaoPorProfissionalDto>> ObterComissoesAsync(
        Guid organizationId,
        DateTime dataInicio,
        DateTime dataFim,
        CancellationToken ct = default)
    {
        // Uma única ida ao banco (Include traz as parcelas junto) — o resto é agregação em
        // memória, porque o cálculo de comissão por parcela depende do método de domínio
        // Fatura.CalcularComissaoSobre, não é expressável em SQL puro sem duplicar a regra aqui.
        var faturas = await _context.Faturas
            .AsNoTracking()
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de FaturamentoSummaryProvider
            .Include(f => f.Parcelas)
            .Where(f => f.OrganizationId == organizationId && f.Status != StatusFatura.Cancelada)
            .ToListAsync(ct);

        var parcelasPagasNoPeriodo =
            from fatura in faturas
            from parcela in fatura.Parcelas
            where parcela.Status == StatusParcela.Paga
                  && parcela.DataPagamento.HasValue
                  && parcela.DataPagamento.Value >= dataInicio
                  && parcela.DataPagamento.Value <= dataFim
            select new
            {
                fatura.ProfissionalId,
                FaturaId = fatura.Id,
                ValorPago = parcela.ValorParcela,
                Comissao = fatura.CalcularComissaoSobre(parcela.ValorParcela)
            };

        return parcelasPagasNoPeriodo
            .GroupBy(l => l.ProfissionalId)
            .Select(g => new ComissaoPorProfissionalDto(
                ProfissionalId: g.Key,
                ValorPagoNoPeriodo: g.Sum(l => l.ValorPago),
                ValorComissao: g.Sum(l => l.Comissao),
                QuantidadeFaturas: g.Select(l => l.FaturaId).Distinct().Count(),
                QuantidadeParcelasPagas: g.Count()))
            .ToList();
    }
}
