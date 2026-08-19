using Identity.Application.Commands.Refresh;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.UnitTests.Commands;

[TestFixture]
public class RefreshTokenCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private Mock<IJwtTokenService> _jwtTokenService;
    private Mock<IRefreshTokenGenerator> _refreshTokenGenerator;
    private Mock<IUnitOfWork> _unitOfWork;
    private RefreshTokenCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _refreshTokenGenerator = new Mock<IRefreshTokenGenerator>();
        _unitOfWork = new Mock<IUnitOfWork>();

        var authOptions = Options.Create(new AuthOptions { RefreshTokenExpirationDays = 7 });

        _handler = new RefreshTokenCommandHandler(
            _userRepository.Object,
            _membershipRepository.Object,
            _refreshTokenRepository.Object,
            _jwtTokenService.Object,
            _refreshTokenGenerator.Object,
            _unitOfWork.Object,
            authOptions);
    }

    [Test]
    public async Task Should_RotateToken_When_ExistingTokenIsActive()
    {
        var organizationId = Guid.NewGuid();
        var user = User.Create("Recepção", "recepcao@clinica.com", "hash").Value;
        var membership = OrganizationMembership.Create(organizationId, user.Id, Role.Recepcao).Value;
        var existingToken = RefreshToken.Create(organizationId, user.Id, "hash-antigo", TimeSpan.FromDays(7));

        _refreshTokenGenerator.Setup(g => g.Hash("plain-antigo")).Returns("hash-antigo");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-antigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, organizationId, membership.Role, membership.BranchId))
            .Returns(("novo-access-token", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-novo", "hash-novo"));

        var result = await _handler.Handle(new RefreshTokenCommand("plain-antigo"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.AccessToken, Is.EqualTo("novo-access-token"));
        Assert.That(result.Value.RefreshToken, Is.EqualTo("plain-novo"));
        Assert.That(existingToken.IsRevoked, Is.True, "token antigo tem que ser revogado na rotação");

        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnInvalido_When_TokenNotFound()
    {
        _refreshTokenGenerator.Setup(g => g.Hash(It.IsAny<string>())).Returns("hash-qualquer");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-qualquer", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await _handler.Handle(new RefreshTokenCommand("token-inexistente"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("RefreshToken.Invalido"));
    }

    [Test]
    public async Task Should_ReturnInvalido_When_TokenIsRevoked()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var revokedToken = RefreshToken.Create(organizationId, userId, "hash-revogado", TimeSpan.FromDays(7));
        revokedToken.Revoke("hash-substituto");

        _refreshTokenGenerator.Setup(g => g.Hash("plain-revogado")).Returns("hash-revogado");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-revogado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-revogado"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("RefreshToken.Invalido"));
    }

    // ── Regressão: detecção de reuso de token já rotacionado (indício de roubo) ─────────────
    [Test]
    public async Task Should_RevokeAllActiveTokensForUser_When_AlreadyRotatedTokenIsReused()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Token revogado (já foi trocado por outro na rotação), mas ainda dentro da validade
        // original — apresentá-lo de novo é indício clássico de token roubado sendo reusado.
        var reusedToken = RefreshToken.Create(organizationId, userId, "hash-reusado", TimeSpan.FromDays(7));
        reusedToken.Revoke("hash-que-o-substituiu");

        _refreshTokenGenerator.Setup(g => g.Hash("plain-reusado")).Returns("hash-reusado");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-reusado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reusedToken);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-reusado"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("RefreshToken.Invalido"));

        _refreshTokenRepository.Verify(r => r.RevokeAllActiveTokensForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once,
            "reuso de token rotacionado tem que derrubar TODAS as sessões do usuário, não só recusar esse token");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never,
            "não deve nem emitir novo par de tokens quando reuso é detectado");
    }

    [Test]
    public async Task Should_ReturnUsuarioInativo_When_UserBehindTokenIsDeactivated()
    {
        var organizationId = Guid.NewGuid();
        var user = User.Create("Recepção", "recepcao2@clinica.com", "hash").Value;
        user.Desativar();
        var existingToken = RefreshToken.Create(organizationId, user.Id, "hash-ativo", TimeSpan.FromDays(7));

        _refreshTokenGenerator.Setup(g => g.Hash("plain-ativo")).Returns("hash-ativo");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-ativo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-ativo"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.UsuarioInativo"));
    }

    // ── Edge Case adicionado por QA (gap: expiração não coberta) ─────────────
    [Test]
    public async Task Should_ReturnInvalido_When_TokenIsExpired()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        // Lifetime negativo => criado já expirado (mesma técnica do RefreshTokenTests)
        var expiredToken = RefreshToken.Create(organizationId, userId, "hash-expirado", TimeSpan.FromSeconds(-1));

        _refreshTokenGenerator.Setup(g => g.Hash("plain-expirado")).Returns("hash-expirado");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-expirado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-expirado"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("RefreshToken.Invalido"));

        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never,
            "handler não deve nem consultar usuário quando o refresh token já falhou por expiração");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Isolamento de organization na rotação (gap: novo par sempre no organization da MEMBERSHIP reeleita, não do token) ─
    [Test]
    public async Task Should_ScopeNewTokenToReelectedMembershipOrganization_When_Rotating()
    {
        // Cenário defensivo: se por qualquer motivo o RefreshToken em base ficou desalinhado do
        // OrganizationId real, o handler tem que confiar na membership reeleita (fonte da
        // verdade pro Role/BranchId atuais), nunca em dado velho do token.
        var driftOrganizationId = Guid.NewGuid(); // valor diferente, simula bug/dados corrompidos
        var user = User.Create("Recepção", "recepcao3@clinica.com", "hash").Value;
        var membership = OrganizationMembership.Create(driftOrganizationId, user.Id, Role.Recepcao).Value;
        var existingToken = RefreshToken.Create(driftOrganizationId, user.Id, "hash-drift", TimeSpan.FromDays(7));

        _refreshTokenGenerator.Setup(g => g.Hash("plain-drift")).Returns("hash-drift");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-drift", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, driftOrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _jwtTokenService.Setup(s => s.GenerateAccessToken(user.Id, driftOrganizationId, membership.Role, membership.BranchId))
            .Returns(("access-scoped", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-novo", "hash-novo"));

        RefreshToken? addedToken = null;
        _refreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((rt, _) => addedToken = rt)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-drift"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(addedToken, Is.Not.Null);
        Assert.That(addedToken!.OrganizationId, Is.EqualTo(driftOrganizationId),
            "novo refresh token tem que ser escopado no organization da MEMBERSHIP reeleita (fonte da verdade)");
        _jwtTokenService.Verify(s => s.GenerateAccessToken(user.Id, driftOrganizationId, membership.Role, membership.BranchId), Times.Once,
            "access token novo carrega o organization_id/role da membership reeleita");
    }

    // ── Task 013/014: refresh falha se a afiliação da org do token não existe mais/foi desativada ──
    [Test]
    public async Task Should_ReturnInvalido_When_MembershipForTokenOrganizationIsMissing()
    {
        var organizationId = Guid.NewGuid();
        var user = User.Create("Recepção", "recepcao4@clinica.com", "hash").Value;
        var existingToken = RefreshToken.Create(organizationId, user.Id, "hash-sem-membership", TimeSpan.FromDays(7));

        _refreshTokenGenerator.Setup(g => g.Hash("plain-sem-membership")).Returns("hash-sem-membership");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-sem-membership", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationMembership?)null);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-sem-membership"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("RefreshToken.Invalido"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnInvalido_When_MembershipForTokenOrganizationIsInactive()
    {
        var organizationId = Guid.NewGuid();
        var user = User.Create("Recepção", "recepcao5@clinica.com", "hash").Value;
        var membership = OrganizationMembership.Create(organizationId, user.Id, Role.Recepcao).Value;
        membership.Desativar();
        var existingToken = RefreshToken.Create(organizationId, user.Id, "hash-inativa", TimeSpan.FromDays(7));

        _refreshTokenGenerator.Setup(g => g.Hash("plain-inativa")).Returns("hash-inativa");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAcrossOrganizationsAsync("hash-inativa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetByUserAndOrganizationAcrossOrganizationsAsync(user.Id, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-inativa"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("RefreshToken.Invalido"));
    }
}
