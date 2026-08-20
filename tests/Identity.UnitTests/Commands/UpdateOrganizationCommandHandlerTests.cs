using Identity.Application.Commands.UpdateOrganization;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Moq;

namespace Identity.UnitTests.Commands;

[TestFixture]
public class UpdateOrganizationCommandHandlerTests
{
    private Mock<IOrganizationRepository> _organizationRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UpdateOrganizationCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _organizationRepository = new Mock<IOrganizationRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateOrganizationCommandHandler(_organizationRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_UpdateFields_When_OrganizationExists()
    {
        var organization = Organization.Create("Clínica Antiga").Value;
        _organizationRepository.Setup(r => r.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);

        var result = await _handler.Handle(
            new UpdateOrganizationCommand(organization.Id, "Clínica Nova", "00.000.000/0001-00", "11999999999", "Rua Nova, 1"),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nome, Is.EqualTo("Clínica Nova"));
        Assert.That(result.Value.Cnpj, Is.EqualTo("00.000.000/0001-00"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_OrganizationDoesNotExist()
    {
        _organizationRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var result = await _handler.Handle(new UpdateOrganizationCommand(Guid.NewGuid(), "Clínica Nova", null, null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Organization.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnNomeObrigatorio_When_NomeIsBlank()
    {
        var organization = Organization.Create("Clínica Antiga").Value;
        _organizationRepository.Setup(r => r.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);

        var result = await _handler.Handle(new UpdateOrganizationCommand(organization.Id, "  ", null, null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Organization.NomeObrigatorio"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
