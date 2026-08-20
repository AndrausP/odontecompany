using Moq;
using Scheduling.Application.Commands.DeactivateProfissional;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class DeactivateProfissionalCommandHandlerTests
{
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private DeactivateProfissionalCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new DeactivateProfissionalCommandHandler(_profissionalRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_Deactivate_When_ProfissionalExists()
    {
        var organizationId = Guid.NewGuid();
        var profissional = Profissional.Criar(organizationId, "Dr. João", "Ortodontia", TipoContrato.Clt).Value;
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissional.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profissional);

        var result = await _handler.Handle(new DeactivateProfissionalCommand(profissional.Id, organizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(profissional.Ativo, Is.False);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_ProfissionalBelongsToDifferentOrganization()
    {
        var profissional = Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt).Value;
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissional.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profissional);

        var result = await _handler.Handle(new DeactivateProfissionalCommand(profissional.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Profissional.NaoEncontrado"));
        Assert.That(profissional.Ativo, Is.True);
    }
}
