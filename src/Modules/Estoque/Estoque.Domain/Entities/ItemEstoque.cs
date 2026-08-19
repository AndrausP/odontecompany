using Estoque.Domain.Errors;
using SharedKernel;

namespace Estoque.Domain.Entities;

/// <summary>
/// Item de estoque (material/insumo odontológico) — módulo transversal citado no doc de
/// arquitetura ("Estoque: controle de materiais e insumos odontológicos, fase posterior").
/// <see cref="BranchId"/> opcional (Fase 5): null = estoque compartilhado do organization inteiro;
/// setado = estoque específico de uma branch da rede. Movimentação de estoque
/// (<see cref="RegistrarEntrada"/>/<see cref="RegistrarSaida"/>) é sempre incremental — nunca
/// seta <see cref="QuantidadeAtual"/> diretamente de fora, pra manter a invariante "nunca fica
/// negativo" garantida em um único lugar.
/// </summary>
public class ItemEstoque : AggregateRoot, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string UnidadeMedida { get; private set; } = string.Empty;
    public decimal QuantidadeAtual { get; private set; }
    public decimal QuantidadeMinima { get; private set; }
    public bool Ativo { get; private set; } = true;

    private ItemEstoque() { } // EF Core

    private ItemEstoque(Guid organizationId, Guid? branchId, string nome, string unidadeMedida, decimal quantidadeMinima)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        Nome = nome;
        UnidadeMedida = unidadeMedida;
        QuantidadeAtual = 0;
        QuantidadeMinima = quantidadeMinima;
        Ativo = true;
    }

    public static Result<ItemEstoque> Create(Guid organizationId, Guid? branchId, string nome, string unidadeMedida, decimal quantidadeMinima)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<ItemEstoque>(DomainErrors.ItemEstoque.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<ItemEstoque>(DomainErrors.ItemEstoque.NomeObrigatorio);

        if (string.IsNullOrWhiteSpace(unidadeMedida))
            return Result.Failure<ItemEstoque>(DomainErrors.ItemEstoque.UnidadeMedidaObrigatoria);

        if (quantidadeMinima < 0)
            return Result.Failure<ItemEstoque>(DomainErrors.ItemEstoque.QuantidadeInvalida);

        return Result.Success(new ItemEstoque(organizationId, branchId, nome.Trim(), unidadeMedida.Trim(), quantidadeMinima));
    }

    public Result RegistrarEntrada(decimal quantidade)
    {
        if (quantidade <= 0)
            return Result.Failure(DomainErrors.ItemEstoque.QuantidadeInvalida);

        QuantidadeAtual += quantidade;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Nunca deixa QuantidadeAtual ficar negativa — saída maior que o disponível é rejeitada, não truncada em zero.</summary>
    public Result RegistrarSaida(decimal quantidade)
    {
        if (quantidade <= 0)
            return Result.Failure(DomainErrors.ItemEstoque.QuantidadeInvalida);

        if (quantidade > QuantidadeAtual)
            return Result.Failure(DomainErrors.ItemEstoque.EstoqueInsuficiente);

        QuantidadeAtual -= quantidade;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Estoque abaixo do mínimo configurado — sinal de reposição, não é erro.</summary>
    public bool EstoqueBaixo => QuantidadeAtual < QuantidadeMinima;

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }

    public void Ativar()
    {
        Ativo = true;
        SetUpdatedAt();
    }
}
