using Moq;
using Scheduling.Application.Commands.DeactivateSala;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class DeactivateSalaCommandHandlerTests
{
    private Mock<ISalaRepository> _salaRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private DeactivateSalaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _salaRepository = new Mock<ISalaRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new DeactivateSalaCommandHandler(_salaRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_Deactivate_When_SalaExists()
    {
        var organizationId = Guid.NewGuid();
        var sala = Sala.Criar(organizationId, "Sala 1").Value;
        _salaRepository.Setup(r => r.GetByIdAsync(sala.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sala);

        var result = await _handler.Handle(new DeactivateSalaCommand(sala.Id, organizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(sala.Ativa, Is.False);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrada_When_SalaBelongsToDifferentOrganization()
    {
        var sala = Sala.Criar(Guid.NewGuid(), "Sala 1").Value;
        _salaRepository.Setup(r => r.GetByIdAsync(sala.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sala);

        var result = await _handler.Handle(new DeactivateSalaCommand(sala.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Sala.NaoEncontrada"));
        Assert.That(sala.Ativa, Is.True);
    }
}
