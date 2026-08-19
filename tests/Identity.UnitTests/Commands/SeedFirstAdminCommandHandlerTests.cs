using Identity.Application.Commands.SeedFirstAdmin;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;

namespace Identity.UnitTests.Commands;

[TestFixture]
public class SeedFirstAdminCommandHandlerTests
{
    private Mock<IOrganizationRepository> _organizationRepository;
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IPasswordHasher> _passwordHasher;
    private Mock<IUnitOfWork> _unitOfWork;
    private SeedFirstAdminCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _organizationRepository = new Mock<IOrganizationRepository>();
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _handler = new SeedFirstAdminCommandHandler(
            _organizationRepository.Object,
            _userRepository.Object,
            _membershipRepository.Object,
            _passwordHasher.Object,
            _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateOrganizationUserAndOwnerMembership_When_NoOrganizationExistsYet()
    {
        _organizationRepository.Setup(r => r.AnyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("Admin@123456")).Returns("hash-admin");

        OrganizationMembership? addedMembership = null;
        _membershipRepository
            .Setup(r => r.AddAsync(It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()))
            .Callback<OrganizationMembership, CancellationToken>((m, _) => addedMembership = m)
            .Returns(Task.CompletedTask);

        var command = new SeedFirstAdminCommand("Clínica Demo", "Administrador", "admin@clinicademo.local", "Admin@123456");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.OrganizationNome, Is.EqualTo("Clínica Demo"));
        Assert.That(result.Value.AdminEmail, Is.EqualTo("admin@clinicademo.local"));

        _organizationRepository.Verify(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(addedMembership, Is.Not.Null, "seed tem que criar a membership Owner ligando admin à organization criada");
        Assert.That(addedMembership!.Role, Is.EqualTo(Role.Owner), "primeiro usuário da organization vira Owner, não Admin");
        Assert.That(addedMembership.OrganizationId, Is.EqualTo(result.Value.OrganizationId));
        Assert.That(addedMembership.UserId, Is.EqualTo(result.Value.AdminUserId));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnJaExecutado_When_OrganizationAlreadyExists()
    {
        _organizationRepository.Setup(r => r.AnyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new SeedFirstAdminCommand("Clínica Demo", "Administrador", "admin@clinicademo.local", "Admin@123456");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Seed.JaExecutado"));

        _organizationRepository.Verify(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Never);
        _membershipRepository.Verify(r => r.AddAsync(It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
