using Identity.Application.Commands.CreateInvite;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.UnitTests.Commands;

/// <summary>Task 016 — Owner/Admin convida um email pra afiliar-se à organization.</summary>
[TestFixture]
public class CreateInviteCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IInviteRepository> _inviteRepository;
    private Mock<IInviteTokenGenerator> _inviteTokenGenerator;
    private Mock<IInviteNotifier> _inviteNotifier;
    private Mock<IUnitOfWork> _unitOfWork;
    private CreateInviteCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _inviteRepository = new Mock<IInviteRepository>();
        _inviteTokenGenerator = new Mock<IInviteTokenGenerator>();
        _inviteNotifier = new Mock<IInviteNotifier>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _inviteTokenGenerator.Setup(g => g.Generate()).Returns(("token-plano", "token-hash"));

        var authOptions = Options.Create(new AuthOptions { InviteExpirationDays = 7 });

        _handler = new CreateInviteCommandHandler(
            _userRepository.Object,
            _membershipRepository.Object,
            _inviteRepository.Object,
            _inviteTokenGenerator.Object,
            _inviteNotifier.Object,
            _unitOfWork.Object,
            authOptions);
    }

    [Test]
    public async Task Should_CreateInvite_And_Notify_When_EmailIsNotMemberAndHasNoPendingInvite()
    {
        var organizationId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();

        _userRepository.Setup(r => r.GetByEmailAsync("novo@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _inviteRepository.Setup(r => r.GetPendingByEmailAndOrganizationAsync("novo@clinica.com", organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        var result = await _handler.Handle(new CreateInviteCommand(organizationId, invitedByUserId, "Novo@Clinica.com", Role.Dentista), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Email, Is.EqualTo("novo@clinica.com"));
        Assert.That(result.Value.Token, Is.EqualTo("token-plano"));

        _inviteRepository.Verify(r => r.AddAsync(
            It.Is<Invite>(i => i.OrganizationId == organizationId && i.Email == "novo@clinica.com" && i.TokenHash == "token-hash"),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _inviteNotifier.Verify(n => n.SendInviteAsync("novo@clinica.com", "token-plano", organizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnEmailJaMembro_When_InvitedEmailIsAlreadyActiveMember()
    {
        var organizationId = Guid.NewGuid();
        var existingUser = User.Create("Já Membro", "membro@clinica.com", "hash").Value;
        var existingMembership = OrganizationMembership.Create(organizationId, existingUser.Id, Role.Dentista).Value;

        _userRepository.Setup(r => r.GetByEmailAsync("membro@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        _membershipRepository
            .Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(existingUser.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMembership);

        var result = await _handler.Handle(new CreateInviteCommand(organizationId, Guid.NewGuid(), "membro@clinica.com", Role.Dentista), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.EmailJaMembro"));
        _inviteRepository.Verify(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_AllowInvite_When_InvitedEmailHasInactiveMembership()
    {
        // Membership INATIVA (ex: removido antes) não bloqueia novo convite — só membership ATIVA conta.
        var organizationId = Guid.NewGuid();
        var existingUser = User.Create("Ex Membro", "exmembro@clinica.com", "hash").Value;
        var inactiveMembership = OrganizationMembership.Create(organizationId, existingUser.Id, Role.Dentista).Value;
        inactiveMembership.Desativar();

        _userRepository.Setup(r => r.GetByEmailAsync("exmembro@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        _membershipRepository
            .Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(existingUser.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveMembership);
        _inviteRepository.Setup(r => r.GetPendingByEmailAndOrganizationAsync("exmembro@clinica.com", organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        var result = await _handler.Handle(new CreateInviteCommand(organizationId, Guid.NewGuid(), "exmembro@clinica.com", Role.Dentista), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task Should_RevokePreviousPendingInvite_When_DuplicateInviteForSameEmailAndOrganization()
    {
        var organizationId = Guid.NewGuid();
        var previousInvite = Invite.Create(organizationId, "duplicado@clinica.com", Role.Recepcao, "hash-antigo", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;

        _userRepository.Setup(r => r.GetByEmailAsync("duplicado@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _inviteRepository.Setup(r => r.GetPendingByEmailAndOrganizationAsync("duplicado@clinica.com", organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousInvite);

        var result = await _handler.Handle(new CreateInviteCommand(organizationId, Guid.NewGuid(), "duplicado@clinica.com", Role.Dentista), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(previousInvite.Status, Is.EqualTo(InviteStatus.Revogado), "convite pendente anterior pro mesmo email+organization é revogado, não duplicado");
    }
}
