using Identity.Application.Commands.Signup;
using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Moq;

namespace Identity.UnitTests.Commands;

/// <summary>Task 015 — signup público. Email duplicado nunca revela se o email existe (409 genérico, mesma anti-enumeração do login).</summary>
[TestFixture]
public class SignupCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IPasswordHasher> _passwordHasher;
    private Mock<IJwtTokenService> _jwtTokenService;
    private Mock<IUnitOfWork> _unitOfWork;
    private SignupCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _handler = new SignupCommandHandler(_userRepository.Object, _passwordHasher.Object, _jwtTokenService.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateUser_And_ReturnTokenWithoutOrganizationAndWithoutRefreshToken_When_EmailIsNew()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("nova@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("Senha@123")).Returns("hash-gerado");
        _jwtTokenService.Setup(s => s.GenerateAccessToken(It.IsAny<Guid>(), null, null, null)).Returns(("access-sem-org", 900));

        var result = await _handler.Handle(new SignupCommand("Novo Usuário", "Nova@Clinica.com", "Senha@123"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Email, Is.EqualTo("nova@clinica.com"));
        Assert.That(result.Value.AccessToken, Is.EqualTo("access-sem-org"));
        Assert.That(result.Value.RefreshToken, Is.Null,
            "signup nunca emite refresh token — RefreshToken é IMustHaveOrganization e o usuário ainda não tem organization nenhuma");

        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnEmailJaCadastrado_When_EmailAlreadyExists()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("existente@clinica.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(new SignupCommand("Alguém", "existente@clinica.com", "Senha@123"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.EmailJaCadastrado"));
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnEmailJaCadastrado_When_UniqueConstraintViolationOnSave()
    {
        // TOCTOU: duas requisições concorrentes com o mesmo email passam pelo EmailExistsAsync
        // antes de qualquer uma comitar. O banco (unique index) é quem garante de verdade.
        _userRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_Users_Email", new Exception("dup")));

        var result = await _handler.Handle(new SignupCommand("Corrida", "corrida@clinica.com", "Senha@123"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.EmailJaCadastrado"));
    }
}
