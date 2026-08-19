namespace Billing.Contracts;

/// <summary>Porta de leitura agregada pro módulo Reporting (task 008) — mesmo padrão dos demais summary providers.</summary>
public interface IFaturamentoSummaryProvider
{
    Task<FaturamentoResumoDto> ObterResumoAsync(Guid organizationId, DateTime dataInicio, DateTime dataFim, CancellationToken ct = default);
}

public sealed record FaturamentoResumoDto(
    decimal ValorTotalFaturado,
    decimal ValorTotalRecebido,
    decimal ValorTotalPendente,
    int QuantidadeFaturas);
