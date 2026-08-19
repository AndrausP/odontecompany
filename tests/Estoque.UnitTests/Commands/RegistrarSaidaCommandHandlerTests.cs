using Estoque.Application.Commands.RegistrarSaida;
using Estoque.Application.Interfaces;
using Estoque.Domain.Entities;
using Moq;

namespace Estoque.UnitTests.Commands;

[TestFixture]
public class RegistrarSaidaCommandHandlerTests
{
    private Mock<IItemEstoqueRepository> _itemRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private RegistrarSaidaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _itemRepository = new Mock<IItemEstoqueRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new RegistrarSaidaCommandHandler(_itemRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_ReturnFailure_When_ItemNotFound()
    {
        _itemRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ItemEstoque?)null);

        var result = await _handler.Handle(new RegistrarSaidaCommand(Guid.NewGuid(), 5), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("ItemEstoque.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnFailure_And_NotSave_When_QuantidadeExceedsStock()
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", 0).Value;
        item.RegistrarEntrada(5);

        _itemRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _handler.Handle(new RegistrarSaidaCommand(item.Id, 10), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("ItemEstoque.EstoqueInsuficiente"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_RegistrarSaida_And_Save_When_QuantidadeIsWithinStock()
    {
        var item = ItemEstoque.Create(Guid.NewGuid(), null, "Luva", "caixa", 0).Value;
        item.RegistrarEntrada(20);

        _itemRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _handler.Handle(new RegistrarSaidaCommand(item.Id, 15), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.QuantidadeAtual, Is.EqualTo(5));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
