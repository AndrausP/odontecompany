using SharedKernel;

namespace Estoque.Domain.Errors;

public static class DomainErrors
{
    public static class ItemEstoque
    {
        public static readonly Error OrganizationInvalido = new("ItemEstoque.OrganizationInvalido", "Organization inválido.");
        public static readonly Error NomeObrigatorio = new("ItemEstoque.NomeObrigatorio", "Nome é obrigatório.");
        public static readonly Error UnidadeMedidaObrigatoria = new("ItemEstoque.UnidadeMedidaObrigatoria", "Unidade de medida é obrigatória.");
        public static readonly Error QuantidadeInvalida = new("ItemEstoque.QuantidadeInvalida", "Quantidade deve ser maior que zero.");
        public static readonly Error EstoqueInsuficiente = new("ItemEstoque.EstoqueInsuficiente", "Quantidade em estoque insuficiente para esta saída.");
        public static readonly Error NaoEncontrado = new("ItemEstoque.NaoEncontrado", "Item de estoque não encontrado.");
        public static readonly Error BranchInvalida = new("ItemEstoque.BranchInvalida", "Branch inválida ou inativa.");
    }
}
