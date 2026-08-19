namespace Billing.Contracts;

public sealed record FaturaDto(
    Guid Id,
    Guid PacienteId,
    Guid? AgendamentoId,
    Guid? ProfissionalId,
    string TipoFatura,
    Guid? ConvenioId,
    decimal ValorTotal,
    string FormaPagamento,
    string Status,
    decimal? ComissaoDentistaPercentual,
    decimal ValorComissao,
    string? ProtocoloConvenio,
    IReadOnlyList<ParcelaDto> Parcelas,
    DateTime CreatedAt);

public sealed record ParcelaDto(
    Guid Id,
    int NumeroParcela,
    decimal ValorParcela,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    string Status);

public sealed record ConvenioDto(Guid Id, string Nome, string? CodigoExterno, bool Ativo);
