using Identity.Application.Commands.AcceptInvite;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;

namespace Identity.UnitTests.Commands;

/// <summary>
/// Task 016 — aceitar convite. Toda falha (token inexistente/expirado/já usado/email não bate)
/// devolve o MESMO erro genérico (Invite.NaoEncontrado) — anti-enumeração.
/// </summary>
[TestFixture]
public class AcceptInviteCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IInviteRepository> _inviteRepository;
    private Mock<IInviteTokenGenerator> _inviteTokenGenerator;
    private Mock<IUnitOfWork> _unitOfWork;
    private AcceptInviteCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _inviteRepository = new Mock<IInviteRepository>();
        _inviteTokenGenerator = new Mock<IInviteTokenGenerator>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _inviteTokenGenerator.Setup(g => g.Hash("token-plano")).Returns("token-hash");

        _handler = new AcceptInviteCommandHandler(
            _userRepository.Object,
            _membershipRepository.Object,
            _inviteRepository.Object,
            _inviteTokenGenerator.Object,
            _unitOfWork.Object);
    }

    private static Invite BuildPendingInvite(Guid organizationId, string email = "convidado@clinica.com", TimeSpan? lifetime = null)
        => Invite.Create(organizationId, email, Role.Dentista, "token-hash", Guid.NewGuid(), lifetime ?? TimeSpan.FromDays(7)).Value;

    [Test]
    public async Task Should_CreateMembership_And_AcceptInvite_When_TokenIsValid()
    {
        var organizationId = Guid.NewGuid();
        var invite = BuildPendingInvite(organizationId);
        var user = User.Create("Convidado", "convidado@clinica.com", "hash").Value;

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository
            .Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationMembership?)null);

        var result = await _handler.Handle(new AcceptInviteCommand(user.Id, "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.OrganizationId, Is.EqualTo(organizationId));
        Assert.That(result.Value.Role, Is.EqualTo(Role.Dentista));
        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Aceito));

        _membershipRepository.Verify(r => r.AddAsync(
            It.Is<OrganizationMembership>(m => m.UserId == user.Id && m.OrganizationId == organizationId && m.Role == Role.Dentista),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_TokenDoesNotExist()
    {
        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        var result = await _handler.Handle(new AcceptInviteCommand(Guid.NewGuid(), "token-inexistente"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_InviteAlreadyAccepted()
    {
        var invite = BuildPendingInvite(Guid.NewGuid());
        invite.Accept();

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);

        var result = await _handler.Handle(new AcceptInviteCommand(Guid.NewGuid(), "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.NaoEncontrado"));
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_InviteWasRevoked()
    {
        var invite = BuildPendingInvite(Guid.NewGuid());
        invite.Revoke();

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);

        var result = await _handler.Handle(new AcceptInviteCommand(Guid.NewGuid(), "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.NaoEncontrado"));
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_And_MarkExpired_When_InviteIsExpired()
    {
        var organizationId = Guid.NewGuid();
        var invite = BuildPendingInvite(organizationId, lifetime: TimeSpan.FromSeconds(-1));

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);

        var result = await _handler.Handle(new AcceptInviteCommand(Guid.NewGuid(), "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.NaoEncontrado"));
        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Expirado), "expiração lazy: marcado no momento em que alguém tenta usar o convite vencido");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _membershipRepository.Verify(r => r.AddAsync(It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_UserEmailDoesNotMatchInviteEmail()
    {
        var organizationId = Guid.NewGuid();
        var invite = BuildPendingInvite(organizationId, email: "convidado@clinica.com");
        var otherUser = User.Create("Outro Usuário", "outro@clinica.com", "hash").Value;

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);
        _userRepository.Setup(r => r.GetByIdAsync(otherUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherUser);

        var result = await _handler.Handle(new AcceptInviteCommand(otherUser.Id, "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.NaoEncontrado"));
        _membershipRepository.Verify(r => r.AddAsync(It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnUsuarioInativo_When_UserIsDeactivated()
    {
        var organizationId = Guid.NewGuid();
        var invite = BuildPendingInvite(organizationId);
        var user = User.Create("Convidado", "convidado@clinica.com", "hash").Value;
        user.Desativar();

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new AcceptInviteCommand(user.Id, "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.UsuarioInativo"));
    }

    [Test]
    public async Task Should_BeIdempotent_When_UserAlreadyHasMembershipInOrganization()
    {
        // Corrida (duas abas aceitando o mesmo convite) — não duplica membership, só fecha o convite.
        var organizationId = Guid.NewGuid();
        var invite = BuildPendingInvite(organizationId);
        var user = User.Create("Convidado", "convidado@clinica.com", "hash").Value;
        var existingMembership = OrganizationMembership.Create(organizationId, user.Id, Role.Admin).Value;

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository
            .Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMembership);

        var result = await _handler.Handle(new AcceptInviteCommand(user.Id, "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Role, Is.EqualTo(Role.Admin), "devolve o papel da membership JÁ existente, não o do convite");
        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Aceito));
        _membershipRepository.Verify(r => r.AddAsync(It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReactivateMembership_When_AcceptingInviteForInactiveMembership()
    {
        // Task 020 — bug latente: membership desativada (ex: Owner removeu, depois re-convidou)
        // não podia ficar presa em Inativo com resposta de sucesso. Convite novo tem Role.Dentista;
        // membership antiga era Role.Admin — reativação deve adotar o papel do convite novo.
        var organizationId = Guid.NewGuid();
        var invite = BuildPendingInvite(organizationId);
        var user = User.Create("Convidado", "convidado@clinica.com", "hash").Value;
        var existingMembership = OrganizationMembership.Create(organizationId, user.Id, Role.Admin).Value;
        existingMembership.Desativar();

        _inviteRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(invite);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository
            .Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMembership);

        var result = await _handler.Handle(new AcceptInviteCommand(user.Id, "token-plano"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(existingMembership.IsAtivo, Is.True, "membership deve sair de Inativo, não ficar presa");
        Assert.That(result.Value.Role, Is.EqualTo(Role.Dentista), "reativação adota o papel do convite novo, não preserva o papel antigo");
        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Aceito));
        _membershipRepository.Verify(r => r.AddAsync(It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()), Times.Never, "reativa a existente, não cria uma nova (quebraria o índice único)");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
