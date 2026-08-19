namespace Reporting.Contracts;

/// <summary>
/// Resposta de <c>GetComissoesPorPeriodoQuery</c> (task 023) — serve tanto a visão "colaborador"
/// (Dentista vendo a própria comissão, <see cref="Linhas"/> com no máximo 1 item) quanto a visão
/// "empresa" (Admin/Owner vendo o agregado da organization, opcionalmente recortado por filial ou
/// classe) — é a MESMA query, a diferença é só quem preenche o filtro (decisão D3 do Tech Lead,
/// docs/tasks/023-query-comissoes-endpoint-rbac.md).
/// </summary>
public sealed record ComissaoResumoDto(
    Guid OrganizationId,
    DateTime DataInicio,
    DateTime DataFim,
    // Período inclusivo nas duas pontas ((DataFim - DataInicio).Days + 1), mínimo 1 — nunca zero.
    int DiasNoPeriodo,
    decimal ValorComissaoTotal,
    decimal ValorPagoTotal,
    int QuantidadeFaturas,
    // Regra de negócio (definição de "dias do período"), calculada no backend — nunca no front.
    decimal MediaDiariaComissao,
    decimal MediaDiariaValorPago,
    IReadOnlyList<ComissaoLinhaDto> Linhas);

/// <summary>Comissão de um profissional (ou do bucket "Não atribuído") dentro do período.</summary>
public sealed record ComissaoLinhaDto(
    // null = fatura sem profissional atribuído — bucket "Não atribuído" (mesmo bucket de
    // Billing.Contracts.ComissaoPorProfissionalDto).
    Guid? ProfissionalId,
    string ProfissionalNome,
    Guid? BranchId,
    string? BranchNome,
    // Role da membership (string, mesma fronteira de ICurrentUserAccessor.Role) — null quando o
    // profissional não tem UserId vinculado ou não há membership correspondente.
    string? Classe,
    decimal ValorPago,
    decimal ValorComissao,
    int QuantidadeFaturas,
    decimal MediaDiariaComissao);
