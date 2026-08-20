using Moq;
using Scheduling.Application.Commands.UpdateProfissional;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class UpdateProfissionalCommandHandlerTests
{
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UpdateProfissionalCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateProfissionalCommandHandler(_profissionalRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_UpdateFields_When_ProfissionalExists()
    {
        var organizationId = Guid.NewGuid();
        var profissional = Profissional.Criar(organizationId, "Dr. Antigo", "Clínico Geral", TipoContrato.Clt).Value;
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissional.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profissional);

        var result = await _handler.Handle(
            new UpdateProfissionalCommand(profissional.Id, organizationId, "Dr. Novo", "Ortodontia", TipoContrato.Pj, 25m, "novo@clinica.com", null),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nome, Is.EqualTo("Dr. Novo"));
        Assert.That(result.Value.TipoContrato, Is.EqualTo("Pj"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_ProfissionalBelongsToDifferentOrganization()
    {
        var profissional = Profissional.Criar(Guid.NewGuid(), "Dr. Antigo", "Clínico Geral", TipoContrato.Clt).Value;
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissional.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profissional);

        var result = await _handler.Handle(
            new UpdateProfissionalCommand(profissional.Id, Guid.NewGuid(), "Dr. Novo", "Ortodontia", TipoContrato.Pj, null, null, null),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Profissional.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
