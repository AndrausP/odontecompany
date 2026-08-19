using Identity.Application.Commands.CreateUser;
using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;
using Tenancy.Contracts;

namespace Identity.UnitTests.Commands;

[TestFixture]
public class CreateUserCommandHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IPasswordHasher> _passwordHasher;
    private Mock<IBranchLookup> _branchLookup;
    private Mock<IUnitOfWork> _unitOfWork;
    private CreateUserCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _branchLookup = new Mock<IBranchLookup>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _handler = new CreateUserCommandHandler(
            _userRepository.Object,
            _membershipRepository.Object,
            _passwordHasher.Object,
            _branchLookup.Object,
            _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateUserAndMembership_When_EmailIsUniqueAndDataIsValid()
    {
        var organizationId = Guid.NewGuid();

        _userRepository.Setup(r => r.EmailExistsAsync("dentista@clinica.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("Senha@123")).Returns("hash-gerado");

        OrganizationMembership? addedMembership = null;
        _membershipRepository
            .Setup(r => r.AddAsync(It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()))
            .Callback<OrganizationMembership, CancellationToken>((m, _) => addedMembership = m)
            .Returns(Task.CompletedTask);

        var command = new CreateUserCommand(organizationId, "Dr. João", "dentista@clinica.com", "Senha@123", Role.Dentista);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Email, Is.EqualTo("dentista@clinica.com"));
        Assert.That(result.Value.Role, Is.EqualTo(Role.Dentista));

        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(addedMembership, Is.Not.Null, "CreateUser tem que criar User + Membership na org do chamador");
        Assert.That(addedMembership!.OrganizationId, Is.EqualTo(organizationId));
        Assert.That(addedMembership.Role, Is.EqualTo(Role.Dentista));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnEmailJaCadastrado_When_EmailAlreadyExists()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("existente@clinica.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateUserCommand(Guid.NewGuid(), "Fulano", "existente@clinica.com", "Senha@123", Role.Recepcao);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.EmailJaCadastrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnMembershipOrganizationInvalido_When_OrganizationIdIsEmpty()
    {
        _userRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-gerado");

        var command = new CreateUserCommand(Guid.Empty, "Fulano", "fulano@clinica.com", "Senha@123", Role.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Membership.OrganizationInvalido"),
            "OrganizationId inválido agora é validado por OrganizationMembership.Create, não por User.Create");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Regressão: TOCTOU entre EmailExistsAsync e SaveChangesAsync ──────────────────────────
    [Test]
    public async Task Should_ReturnEmailJaCadastrado_When_UniqueConstraintViolationHappensOnSave()
    {
        // Simula duas requisições concorrentes: ambas passam no EmailExistsAsync (false) antes
        // de qualquer uma commitar; a segunda a persistir estoura unique constraint no banco.
        // A Infrastructure traduz isso em UniqueConstraintViolationException — o handler tem
        // que devolver Result.Failure tipado, nunca deixar a exception subir como 500 cru.
        _userRepository.Setup(r => r.EmailExistsAsync("concorrente@clinica.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-gerado");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_Users_Email", new Exception("unique violation")));

        var command = new CreateUserCommand(Guid.NewGuid(), "Fulano", "concorrente@clinica.com", "Senha@123", Role.Recepcao);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.EmailJaCadastrado"));
    }

    // ── Fase 5: BranchId opcional, validado quando informado (agora vive na Membership) ─────
    [Test]
    public async Task Should_ReturnBranchInvalida_When_BranchIdIsProvided_But_DoesNotExist()
    {
        _userRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _branchLookup.Setup(u => u.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateUserCommand(Guid.NewGuid(), "Recepcionista", "recepcao@clinica.com", "Senha@123", Role.Recepcao, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.BranchInvalida"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_CreateUser_When_BranchIdIsProvided_And_Exists()
    {
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        _userRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _branchLookup.Setup(u => u.ExistsAsync(organizationId, branchId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-gerado");

        var command = new CreateUserCommand(organizationId, "Recepcionista", "recepcao@clinica.com", "Senha@123", Role.Recepcao, branchId);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.BranchId, Is.EqualTo(branchId));
    }
}
