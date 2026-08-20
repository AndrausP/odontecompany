using Identity.Application.Commands.ForgotPassword;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.UnitTests.Commands;

/// <summary>Auditoria pré-venda — recuperação de senha (antes disso não existia nenhuma).</summary>
[TestFixture]
public class ForgotPasswordCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<IPasswordResetTokenRepository> _tokenRepository = null!;
    private Mock<IPasswordResetTokenGenerator> _tokenGenerator = null!;
    private Mock<IPasswordResetNotifier> _notifier = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private ForgotPasswordCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _tokenRepository = new Mock<IPasswordResetTokenRepository>();
        _tokenGenerator = new Mock<IPasswordResetTokenGenerator>();
        _notifier = new Mock<IPasswordResetNotifier>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _tokenGenerator.Setup(g => g.Generate()).Returns(("token-plano", "token-hash"));

        var authOptions = Options.Create(new AuthOptions { PasswordResetExpirationHours = 2 });

        _handler = new ForgotPasswordCommandHandler(
            _userRepository.Object,
            _tokenRepository.Object,
            _tokenGenerator.Object,
            _notifier.Object,
            _unitOfWork.Object,
            authOptions);
    }

    [Test]
    public async Task Should_CreateTokenAndNotify_When_EmailExistsAndUserIsAtivo()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash").Value;
        _userRepository.Setup(r => r.GetByEmailAsync("ana@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new ForgotPasswordCommand("ana@clinica.com"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _tokenRepository.Verify(r => r.InvalidateActiveTokensForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(n => n.SendResetLinkAsync(user.Email, "token-plano", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Anti-enumeração: email inexistente devolve o MESMO sucesso, sem criar token nem notificar.</summary>
    [Test]
    public async Task Should_ReturnSuccessWithoutSideEffects_When_EmailDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.Handle(new ForgotPasswordCommand("naoexiste@clinica.com"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _notifier.Verify(n => n.SendResetLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnSuccessWithoutSideEffects_When_UserIsInativo()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash").Value;
        user.Desativar();
        _userRepository.Setup(r => r.GetByEmailAsync("ana@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new ForgotPasswordCommand("ana@clinica.com"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
