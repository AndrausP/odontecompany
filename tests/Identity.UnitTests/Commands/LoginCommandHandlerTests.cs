using Identity.Application.Commands.Login;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.UnitTests.Commands;

[TestFixture]
public class LoginCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IOrganizationRepository> _organizationRepository;
    private Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private Mock<IPasswordHasher> _passwordHasher;
    private Mock<IJwtTokenService> _jwtTokenService;
    private Mock<IRefreshTokenGenerator> _refreshTokenGenerator;
    private Mock<IUnitOfWork> _unitOfWork;
    private LoginCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _organizationRepository = new Mock<IOrganizationRepository>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _refreshTokenGenerator = new Mock<IRefreshTokenGenerator>();
        _unitOfWork = new Mock<IUnitOfWork>();

        // Default: organization sempre ativo, salvo quando o teste sobrescreve explicitamente.
        _organizationRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildActiveOrganization());

        var authOptions = Options.Create(new AuthOptions { RefreshTokenExpirationDays = 7 });

        _handler = new LoginCommandHandler(
            _userRepository.Object,
            _membershipRepository.Object,
            _organizationRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            _refreshTokenGenerator.Object,
            _unitOfWork.Object,
            authOptions);
    }

    private static User BuildActiveUser(string email = "dentista@clinica.com")
        => User.Create("Dra. Ana", email, "hash-armazenado").Value;

    private static OrganizationMembership BuildActiveMembership(Guid organizationId, Guid userId, Role role = Role.Dentista)
        => OrganizationMembership.Create(organizationId, userId, role).Value;

    private static Organization BuildActiveOrganization() => Organization.Create("Clínica Teste").Value;

    [Test]
    public async Task Should_ReturnTokens_When_CredentialsAreValid()
    {
        var organizationId = Guid.NewGuid();
        var user = BuildActiveUser();
        var membership = BuildActiveMembership(organizationId, user.Id);

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("senha-correta", user.PasswordHash)).Returns(true);
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([membership]);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, membership.OrganizationId, membership.Role, membership.BranchId))
            .Returns(("access-token-fake", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-refresh", "hash-refresh"));

        var result = await _handler.Handle(new LoginCommand(user.Email, "senha-correta"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.AccessToken, Is.EqualTo("access-token-fake"));
        Assert.That(result.Value.RefreshToken, Is.EqualTo("plain-refresh"));
        Assert.That(result.Value.ExpiresIn, Is.EqualTo(900));

        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnCredenciaisInvalidas_When_UserNotFound()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new LoginCommand("naoexiste@clinica.com", "qualquer"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.CredenciaisInvalidas"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_CallPasswordHasherVerify_When_UserNotFound_ToAvoidTimingEnumeration()
    {
        // Regressão do bug de timing side-channel: Verify() tem que rodar mesmo pra email
        // inexistente (contra um hash dummy), senão dá pra enumerar email por tempo de resposta.
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-dummy-gerado");

        await _handler.Handle(new LoginCommand("naoexiste@clinica.com", "qualquer"), CancellationToken.None);

        _passwordHasher.Verify(h => h.Verify("qualquer", It.IsAny<string>()), Times.Once,
            "Verify precisa rodar mesmo com usuário inexistente, contra um hash dummy fixo");
    }

    [Test]
    public async Task Should_ReturnCredenciaisInvalidas_When_PasswordIsWrong()
    {
        var user = BuildActiveUser();

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        var result = await _handler.Handle(new LoginCommand(user.Email, "senha-errada"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.CredenciaisInvalidas"));
    }

    [Test]
    public async Task Should_ReturnUsuarioInativo_When_UserIsDeactivated()
    {
        var user = BuildActiveUser();
        user.Desativar();

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(true);

        var result = await _handler.Handle(new LoginCommand(user.Email, "senha-correta"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.UsuarioInativo"));
    }

    [Test]
    public async Task Should_ReturnOrganizationInativo_When_OrganizationIsDeactivated()
    {
        var organizationId = Guid.NewGuid();
        var user = BuildActiveUser();
        var membership = BuildActiveMembership(organizationId, user.Id);
        var organization = BuildActiveOrganization();
        organization.Desativar();

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(true);
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([membership]);
        _organizationRepository.Setup(r => r.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await _handler.Handle(new LoginCommand(user.Email, "senha-correta"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Organization.Inativo"));
    }

    // ── Isolamento multi-organization (gap: nada garantia que o refresh emitido carrega a org da membership) ─
    [Test]
    public async Task Should_ScopeRefreshTokenToElectedOrganization_When_LoginSucceeds()
    {
        var organizationId = Guid.NewGuid();
        var user = BuildActiveUser("isolado@clinica.com");
        var membership = BuildActiveMembership(organizationId, user.Id);

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("senha-correta", user.PasswordHash)).Returns(true);
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([membership]);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, organizationId, membership.Role, membership.BranchId))
            .Returns(("access-token-fake", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-refresh", "hash-refresh"));

        RefreshToken? addedToken = null;
        _refreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((rt, _) => addedToken = rt)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new LoginCommand(user.Email, "senha-correta"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(addedToken, Is.Not.Null);
        Assert.That(addedToken!.OrganizationId, Is.EqualTo(organizationId),
            "refresh token persistido tem que carregar o organization_id da membership eleita — jamais vaza pra outra organization");
        Assert.That(addedToken.UserId, Is.EqualTo(user.Id));
        Assert.That(addedToken.TokenHash, Is.EqualTo("hash-refresh"),
            "persistir o HASH, nunca o valor em claro (critério de aceite da task)");
        _jwtTokenService.Verify(s => s.GenerateAccessToken(user.Id, organizationId, membership.Role, membership.BranchId), Times.Once,
            "access token emitido carrega o organization_id da membership eleita como claim");
    }

    // ── Task 014, item 1: usuário sem NENHUMA organization ainda recebe token válido, sem org ──
    [Test]
    public async Task Should_ReturnTokenWithoutOrganization_And_NoRefreshToken_When_UserHasZeroActiveMemberships()
    {
        var user = BuildActiveUser("solo@clinica.com");

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("senha-correta", user.PasswordHash)).Returns(true);
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, null, null, null))
            .Returns(("access-token-sem-org", 900));

        var result = await _handler.Handle(new LoginCommand(user.Email, "senha-correta"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.AccessToken, Is.EqualTo("access-token-sem-org"));
        Assert.That(result.Value.RefreshToken, Is.Null,
            "sem organization ativa não existe refresh token pra emitir — RefreshToken é IMustHaveOrganization");

        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Task 014, item 1: eleição determinística — membership mais antiga, não a primeira da lista ──
    [Test]
    public async Task Should_ElectOldestMembership_When_UserHasMultipleActiveMemberships()
    {
        var user = BuildActiveUser("multiorg@clinica.com");
        var oldestOrganizationId = Guid.NewGuid();
        var newestOrganizationId = Guid.NewGuid();

        var oldestMembership = BuildActiveMembership(oldestOrganizationId, user.Id, Role.Owner);
        Thread.Sleep(10); // garante CreatedAt estritamente maior na segunda membership
        var newestMembership = BuildActiveMembership(newestOrganizationId, user.Id, Role.Admin);

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("senha-correta", user.PasswordHash)).Returns(true);
        // Devolve fora de ordem de propósito — a eleição tem que ordenar por CreatedAt, não confiar na ordem da lista.
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([newestMembership, oldestMembership]);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, oldestOrganizationId, Role.Owner, null))
            .Returns(("access-oldest-org", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-refresh", "hash-refresh"));

        var result = await _handler.Handle(new LoginCommand(user.Email, "senha-correta"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.AccessToken, Is.EqualTo("access-oldest-org"));
        _jwtTokenService.Verify(s => s.GenerateAccessToken(user.Id, oldestOrganizationId, Role.Owner, null), Times.Once,
            "eleição determinística: a membership mais antiga vence, não a primeira do array devolvido pelo repositório");
    }
}
