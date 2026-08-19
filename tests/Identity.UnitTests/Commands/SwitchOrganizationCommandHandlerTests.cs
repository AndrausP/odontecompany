using Identity.Application.Commands.SwitchOrganization;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.UnitTests.Commands;

/// <summary>
/// Task 014 — troca de organization ativa sem novo login. Sem cobertura de teste até esta
/// sessão (o endpoint/handler foi implementado mas ficou sem suite própria).
/// </summary>
[TestFixture]
public class SwitchOrganizationCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IOrganizationRepository> _organizationRepository;
    private Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private Mock<IJwtTokenService> _jwtTokenService;
    private Mock<IRefreshTokenGenerator> _refreshTokenGenerator;
    private Mock<IUnitOfWork> _unitOfWork;
    private SwitchOrganizationCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _organizationRepository = new Mock<IOrganizationRepository>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _refreshTokenGenerator = new Mock<IRefreshTokenGenerator>();
        _unitOfWork = new Mock<IUnitOfWork>();

        var authOptions = Options.Create(new AuthOptions { RefreshTokenExpirationDays = 7 });

        _handler = new SwitchOrganizationCommandHandler(
            _userRepository.Object,
            _membershipRepository.Object,
            _organizationRepository.Object,
            _refreshTokenRepository.Object,
            _jwtTokenService.Object,
            _refreshTokenGenerator.Object,
            _unitOfWork.Object,
            authOptions);
    }

    [Test]
    public async Task Should_ReturnNewTokens_When_UserHasActiveMembershipInTargetOrganization()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash").Value;
        var targetOrganizationId = Guid.NewGuid();
        var membership = OrganizationMembership.Create(targetOrganizationId, user.Id, Role.Admin).Value;
        var organization = Organization.Create("Clínica B").Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, targetOrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _organizationRepository.Setup(r => r.GetByIdAsync(targetOrganizationId, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, targetOrganizationId, Role.Admin, null))
            .Returns(("access-nova-org", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-novo", "hash-novo"));

        var result = await _handler.Handle(new SwitchOrganizationCommand(user.Id, targetOrganizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.AccessToken, Is.EqualTo("access-nova-org"));
        Assert.That(result.Value.RefreshToken, Is.EqualTo("plain-novo"));

        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnMembershipNaoEncontrada_When_UserHasNoMembershipInTargetOrganization()
    {
        var user = User.Create("Dra. Ana", "ana2@clinica.com", "hash").Value;
        var targetOrganizationId = Guid.NewGuid();

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, targetOrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationMembership?)null);

        var result = await _handler.Handle(new SwitchOrganizationCommand(user.Id, targetOrganizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Membership.NaoEncontrada"),
            "usuário sem afiliação na org pedida tem que ser 403 (controller mapeia), nunca revelar se a org existe");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnMembershipNaoEncontrada_When_MembershipInTargetOrganizationIsInactive()
    {
        var user = User.Create("Dra. Ana", "ana3@clinica.com", "hash").Value;
        var targetOrganizationId = Guid.NewGuid();
        var membership = OrganizationMembership.Create(targetOrganizationId, user.Id, Role.Admin).Value;
        membership.Desativar();

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, targetOrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        var result = await _handler.Handle(new SwitchOrganizationCommand(user.Id, targetOrganizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Membership.NaoEncontrada"));
    }

    [Test]
    public async Task Should_ReturnUsuarioInativo_When_UserIsDeactivated()
    {
        var user = User.Create("Dra. Ana", "ana4@clinica.com", "hash").Value;
        user.Desativar();

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new SwitchOrganizationCommand(user.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.UsuarioInativo"));
        _membershipRepository.Verify(
            r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Should_ReturnOrganizationInativo_When_TargetOrganizationIsDeactivated()
    {
        var user = User.Create("Dra. Ana", "ana5@clinica.com", "hash").Value;
        var targetOrganizationId = Guid.NewGuid();
        var membership = OrganizationMembership.Create(targetOrganizationId, user.Id, Role.Admin).Value;
        var organization = Organization.Create("Clínica Inativa").Value;
        organization.Desativar();

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, targetOrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _organizationRepository.Setup(r => r.GetByIdAsync(targetOrganizationId, It.IsAny<CancellationToken>())).ReturnsAsync(organization);

        var result = await _handler.Handle(new SwitchOrganizationCommand(user.Id, targetOrganizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Organization.Inativo"));
    }

    // ── Task 014, item 3: token anterior não é revogado — troca de org não invalida sessão antiga ──
    [Test]
    public async Task Should_NotRevokeAnyExistingToken_When_Switching()
    {
        var user = User.Create("Dra. Ana", "ana6@clinica.com", "hash").Value;
        var targetOrganizationId = Guid.NewGuid();
        var membership = OrganizationMembership.Create(targetOrganizationId, user.Id, Role.Recepcao).Value;
        var organization = Organization.Create("Clínica C").Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, targetOrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _organizationRepository.Setup(r => r.GetByIdAsync(targetOrganizationId, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, targetOrganizationId, Role.Recepcao, null))
            .Returns(("access-c", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-c", "hash-c"));

        await _handler.Handle(new SwitchOrganizationCommand(user.Id, targetOrganizationId), CancellationToken.None);

        _refreshTokenRepository.Verify(r => r.RevokeAllActiveTokensForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never,
            "switch-organization não revoga token anterior — expira sozinho (decisão do Architect, task 014)");
    }
}
