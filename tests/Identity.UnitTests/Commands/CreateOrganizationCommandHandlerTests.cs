using Identity.Application.Commands.CreateOrganization;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.UnitTests.Commands;

/// <summary>Task 015 — usuário autenticado (com ou sem organization ativa) cria organization e vira Owner automaticamente.</summary>
[TestFixture]
public class CreateOrganizationCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationRepository> _organizationRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private Mock<IJwtTokenService> _jwtTokenService;
    private Mock<IRefreshTokenGenerator> _refreshTokenGenerator;
    private Mock<IUnitOfWork> _unitOfWork;
    private CreateOrganizationCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _organizationRepository = new Mock<IOrganizationRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _refreshTokenGenerator = new Mock<IRefreshTokenGenerator>();
        _unitOfWork = new Mock<IUnitOfWork>();

        var authOptions = Options.Create(new AuthOptions { RefreshTokenExpirationDays = 7 });

        _handler = new CreateOrganizationCommandHandler(
            _userRepository.Object,
            _organizationRepository.Object,
            _membershipRepository.Object,
            _refreshTokenRepository.Object,
            _jwtTokenService.Object,
            _refreshTokenGenerator.Object,
            _unitOfWork.Object,
            authOptions);
    }

    [Test]
    public async Task Should_CreateOrganizationAndOwnerMembership_And_ReturnScopedTokens_When_UserIsValid()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash").Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _jwtTokenService
            .Setup(s => s.GenerateAccessToken(user.Id, It.IsAny<Guid>(), Role.Owner, null))
            .Returns(("access-owner", 900));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns(("plain-refresh", "hash-refresh"));

        var result = await _handler.Handle(new CreateOrganizationCommand(user.Id, "Clínica Nova"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.OrganizationName, Is.EqualTo("Clínica Nova"));
        Assert.That(result.Value.AccessToken, Is.EqualTo("access-owner"));
        Assert.That(result.Value.RefreshToken, Is.EqualTo("plain-refresh"));

        _organizationRepository.Verify(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Once);

        _membershipRepository.Verify(r => r.AddAsync(
            It.Is<OrganizationMembership>(m => m.UserId == user.Id && m.Role == Role.Owner && m.OrganizationId == result.Value.OrganizationId),
            It.IsAny<CancellationToken>()), Times.Once);

        _refreshTokenRepository.Verify(r => r.AddAsync(
            It.Is<RefreshToken>(rt => rt.OrganizationId == result.Value.OrganizationId && rt.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnUsuarioInativo_When_UserNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.Handle(new CreateOrganizationCommand(Guid.NewGuid(), "Clínica"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.UsuarioInativo"));
        _organizationRepository.Verify(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnUsuarioInativo_When_UserIsDeactivated()
    {
        var user = User.Create("Dra. Ana", "inativa@clinica.com", "hash").Value;
        user.Desativar();

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new CreateOrganizationCommand(user.Id, "Clínica"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.UsuarioInativo"));
    }
}
