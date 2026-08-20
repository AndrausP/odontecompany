using Moq;
using Scheduling.Application.Commands.LinkProfissionalUser;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.UnitTests.Commands;

/// <summary>
/// Auditoria pré-venda (task 042) — link automático entre o usuário que aceitou um convite e o(s)
/// profissional(is) que estavam esperando esse email.
/// </summary>
[TestFixture]
public class LinkProfissionalUserCommandHandlerTests
{
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private LinkProfissionalUserCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new LinkProfissionalUserCommandHandler(_profissionalRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_LinkUserId_When_PendingProfissionalMatchesEmail()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var profissional = Profissional.Criar(organizationId, "Dra. Camila", "Ortodontia", TipoContrato.Pj, email: "camila@clinica.com").Value;

        _profissionalRepository
            .Setup(r => r.ListPendentesPorEmailAcrossOrganizationsAsync(organizationId, "camila@clinica.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { profissional });

        var result = await _handler.Handle(new LinkProfissionalUserCommand(organizationId, "camila@clinica.com", userId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(profissional.UserId, Is.EqualTo(userId));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnSuccessWithoutSideEffects_When_NoProfissionalPendingForEmail()
    {
        var organizationId = Guid.NewGuid();
        _profissionalRepository
            .Setup(r => r.ListPendentesPorEmailAcrossOrganizationsAsync(organizationId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Profissional>());

        var result = await _handler.Handle(new LinkProfissionalUserCommand(organizationId, "recepcao@clinica.com", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_LinkAllMatches_When_MultiplePendingProfissionaisShareEmail()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var profA = Profissional.Criar(organizationId, "Dr. A", "Clínico Geral", TipoContrato.Clt, email: "dupla@clinica.com").Value;
        var profB = Profissional.Criar(organizationId, "Dr. B", "Endodontia", TipoContrato.Autonomo, email: "dupla@clinica.com").Value;

        _profissionalRepository
            .Setup(r => r.ListPendentesPorEmailAcrossOrganizationsAsync(organizationId, "dupla@clinica.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { profA, profB });

        var result = await _handler.Handle(new LinkProfissionalUserCommand(organizationId, "dupla@clinica.com", userId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(profA.UserId, Is.EqualTo(userId));
        Assert.That(profB.UserId, Is.EqualTo(userId));
    }
}
