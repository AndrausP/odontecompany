namespace Billing.Contracts;

/// <summary>
/// Porta de leitura agregada pro módulo Reporting (task 023) compor "quanto de comissão cada
/// profissional recebeu, em qual filial, sob qual classe, num período" — regime de CAIXA: só
/// conta parcela com <c>Status == Paga</c> e <c>DataPagamento</c> dentro do período informado,
/// nunca o <c>ValorTotal</c> da fatura inteira (essa é <c>Fatura.CalcularValorComissao</c>,
/// regime de competência, usada noutro lugar). Mesmo padrão de <see cref="IFaturamentoSummaryProvider"/>.
/// </summary>
public interface IComissaoSummaryProvider
{
    /// <summary>
    /// Comissão por profissional no período — batch, sem N+1: devolve todos os profissionais
    /// com parcela paga no período (inclusive bucket "Não atribuído") numa única chamada.
    /// </summary>
    Task<IReadOnlyList<ComissaoPorProfissionalDto>> ObterComissoesAsync(
        Guid organizationId,
        DateTime dataInicio,
        DateTime dataFim,
        CancellationToken ct = default);
}

public sealed record ComissaoPorProfissionalDto(
    Guid? ProfissionalId,          // null = fatura sem profissional atribuído (bucket "Não atribuído")
    decimal ValorPagoNoPeriodo,    // soma das parcelas pagas no período
    decimal ValorComissao,         // comissão proporcional ao valor pago (regime de caixa)
    int QuantidadeFaturas,         // faturas distintas que tiveram parcela paga no período
    int QuantidadeParcelasPagas);
