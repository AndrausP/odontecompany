namespace Estoque.Contracts;

public sealed record ItemEstoqueDto(
    Guid Id,
    Guid? BranchId,
    string Nome,
    string UnidadeMedida,
    decimal QuantidadeAtual,
    decimal QuantidadeMinima,
    bool EstoqueBaixo,
    bool Ativo);
