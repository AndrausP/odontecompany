using Estoque.Domain.Entities;

namespace Estoque.UnitTests.Domain;

[TestFixture]
public class ItemEstoqueTests
{
    [Test]
    public void Should_CreateItem_When_DataIsValid()
    {
        var result = ItemEstoque.Create(Guid.NewGuid(), null, "Luva de Procedimento", "caixa", 10);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.QuantidadeAtual, Is.EqualTo(0));
    }

    [Test]
    public void Should_ReturnFailure_When_NomeIsEmpty()
    {
        var result = ItemEstoque.Create(Guid.NewGuid(), null, "   ", "caixa", 10);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("ItemEstoque.NomeObrigatorio"));
    }

    [Test]
    public void Should_IncreaseQuantidadeAtual_When_RegistrarEntradaIsCalled()
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", 0).Value;

        var result = item.RegistrarEntrada(50);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(item.QuantidadeAtual, Is.EqualTo(50));
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Should_ReturnFailure_When_RegistrarEntrada_QuantidadeIsZeroOrNegative(decimal quantidade)
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", 0).Value;

        var result = item.RegistrarEntrada(quantidade);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("ItemEstoque.QuantidadeInvalida"));
    }

    [Test]
    public void Should_DecreaseQuantidadeAtual_When_RegistrarSaidaIsWithinStock()
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", 0).Value;
        item.RegistrarEntrada(50);

        var result = item.RegistrarSaida(30);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(item.QuantidadeAtual, Is.EqualTo(20));
    }

    [Test]
    public void Should_ReturnFailure_And_NeverGoNegative_When_RegistrarSaidaExceedsStock()
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", 0).Value;
        item.RegistrarEntrada(10);

        var result = item.RegistrarSaida(15);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("ItemEstoque.EstoqueInsuficiente"));
        Assert.That(item.QuantidadeAtual, Is.EqualTo(10), "quantidade não pode ter sido alterada numa saída rejeitada");
    }

    [Test]
    public void Should_ReturnTrueForEstoqueBaixo_When_QuantidadeAtualIsBelowMinima()
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", quantidadeMinima: 20).Value;
        item.RegistrarEntrada(5);

        Assert.That(item.EstoqueBaixo, Is.True);
    }

    [Test]
    public void Should_ReturnFalseForEstoqueBaixo_When_QuantidadeAtualIsAtOrAboveMinima()
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", quantidadeMinima: 20).Value;
        item.RegistrarEntrada(20);

        Assert.That(item.EstoqueBaixo, Is.False);
    }
}
