using Identity.Application.Commands.ResetPassword;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Moq;

namespace Identity.UnitTests.Commands;

[TestFixture]
public class ResetPasswordCommandHandlerTests
{
    private Mock<IPasswordResetTokenRepository> _tokenRepository = null!;
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<IRefreshTokenRepository> _refreshTokenRepository = null!;
    private Mock<IPasswordResetTokenGenerator> _tokenGenerator = null!;
    private Mock<IPasswordHasher> _passwordHasher = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private ResetPasswordCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _tokenRepository = new Mock<IPasswordResetTokenRepository>();
        _userRepository = new Mock<IUserRepository>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _tokenGenerator = new Mock<IPasswordResetTokenGenerator>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _tokenGenerator.Setup(g => g.Hash("token-plano")).Returns("token-hash");
        _passwordHasher.Setup(h => h.Hash("Senha@Nova123")).Returns("novo-hash");

        _handler = new ResetPasswordCommandHandler(
            _tokenRepository.Object,
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _tokenGenerator.Object,
            _passwordHasher.Object,
            _unitOfWork.Object);
    }

    [Test]
    public async Task Should_ChangePasswordAndRevokeAllSessions_When_TokenIsValid()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash-antigo").Value;
        var token = PasswordResetToken.Create(user.Id, "token-hash", TimeSpan.FromHours(2)).Value;

        _tokenRepository.Setup(r => r.GetByTokenHashAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new ResetPasswordCommand("token-plano", "Senha@Nova123"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(user.PasswordHash, Is.EqualTo("novo-hash"));
        Assert.That(token.IsUsed, Is.True);
        _refreshTokenRepository.Verify(r => r.RevokeAllActiveTokensForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnTokenInvalido_When_TokenDoesNotExist()
    {
        _tokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PasswordResetToken?)null);

        var result = await _handler.Handle(new ResetPasswordCommand("token-plano", "Senha@Nova123"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("PasswordReset.TokenInvalido"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnTokenInvalido_When_TokenAlreadyUsed()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash-antigo").Value;
        var token = PasswordResetToken.Create(user.Id, "token-hash", TimeSpan.FromHours(2)).Value;
        token.MarkUsed();

        _tokenRepository.Setup(r => r.GetByTokenHashAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var result = await _handler.Handle(new ResetPasswordCommand("token-plano", "Senha@Nova123"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("PasswordReset.TokenInvalido"));
    }

    [Test]
    public async Task Should_ReturnTokenInvalido_When_TokenIsExpired()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash-antigo").Value;
        var token = PasswordResetToken.Create(user.Id, "token-hash", TimeSpan.FromHours(-1)).Value; // já expirado

        _tokenRepository.Setup(r => r.GetByTokenHashAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var result = await _handler.Handle(new ResetPasswordCommand("token-plano", "Senha@Nova123"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("PasswordReset.TokenInvalido"));
    }
}
