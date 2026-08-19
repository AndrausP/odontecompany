using Billing.Contracts;
using Billing.Domain.Entities;

namespace Billing.Application.Mapping;

public static class FaturaMappingExtensions
{
    public static FaturaDto ToDto(this Fatura fatura)
        => new(
            fatura.Id,
            fatura.PacienteId,
            fatura.AgendamentoId,
            fatura.ProfissionalId,
            fatura.TipoFatura.ToString(),
            fatura.ConvenioId,
            fatura.ValorTotal,
            fatura.FormaPagamento.ToString(),
            fatura.Status.ToString(),
            fatura.ComissaoDentistaPercentual,
            fatura.CalcularValorComissao(),
            fatura.ProtocoloConvenio,
            fatura.Parcelas.Select(p => p.ToDto()).ToList(),
            fatura.CreatedAt);

    public static ParcelaDto ToDto(this Parcela parcela)
        => new(
            parcela.Id,
            parcela.NumeroParcela,
            parcela.ValorParcela,
            parcela.DataVencimento,
            parcela.DataPagamento,
            parcela.Status.ToString());

    public static ConvenioDto ToDto(this Convenio convenio)
        => new(convenio.Id, convenio.Nome, convenio.CodigoExterno, convenio.Ativo);
}
