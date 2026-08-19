using Estoque.Contracts;
using Estoque.Domain.Entities;

namespace Estoque.Application.Mapping;

public static class ItemEstoqueMappingExtensions
{
    public static ItemEstoqueDto ToDto(this ItemEstoque item)
        => new(item.Id, item.BranchId, item.Nome, item.UnidadeMedida, item.QuantidadeAtual, item.QuantidadeMinima, item.EstoqueBaixo, item.Ativo);
}
