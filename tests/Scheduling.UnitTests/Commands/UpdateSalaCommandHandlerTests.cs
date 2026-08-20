using Moq;
using Scheduling.Application.Commands.UpdateSala;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class UpdateSalaCommandHandlerTests
{
    private Mock<ISalaRepository> _salaRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UpdateSalaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _salaRepository = new Mock<ISalaRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateSalaCommandHandler(_salaRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_UpdateFields_When_SalaExists()
    {
        var organizationId = Guid.NewGuid();
        var sala = Sala.Criar(organizationId, "Sala 1").Value;
        _salaRepository.Setup(r => r.GetByIdAsync(sala.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sala);

        var result = await _handler.Handle(new UpdateSalaCommand(sala.Id, organizationId, "Sala 1 Reformada", 4, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nome, Is.EqualTo("Sala 1 Reformada"));
        Assert.That(result.Value.CapacidadeMaxima, Is.EqualTo(4));
    }

    [Test]
    public async Task Should_ReturnNaoEncontrada_When_SalaBelongsToDifferentOrganization()
    {
        var sala = Sala.Criar(Guid.NewGuid(), "Sala 1").Value;
        _salaRepository.Setup(r => r.GetByIdAsync(sala.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sala);

        var result = await _handler.Handle(new UpdateSalaCommand(sala.Id, Guid.NewGuid(), "Sala 1", null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Sala.NaoEncontrada"));
    }
}
