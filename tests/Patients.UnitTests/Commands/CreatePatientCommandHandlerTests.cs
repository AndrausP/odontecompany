using Moq;
using Patients.Application.Commands.CreatePatient;
using Patients.Application.Exceptions;
using Patients.Application.Interfaces;
using Patients.Domain.Entities;

namespace Patients.UnitTests.Commands;

[TestFixture]
public class CreatePatientCommandHandlerTests
{
    private const string ValidCpf = "52998224725";
    private static readonly DateTime ValidDataNascimento = new(1990, 5, 20);

    private Mock<IPatientRepository> _patientRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreatePatientCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _patientRepository = new Mock<IPatientRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreatePatientCommandHandler(_patientRepository.Object, _unitOfWork.Object);
    }

    private static CreatePatientCommand ValidCommand(Guid? organizationId = null, bool consentimentoLgpd = true) => new(
        organizationId ?? Guid.NewGuid(),
        "Maria Silva",
        ValidCpf,
        ValidDataNascimento,
        "11999999999",
        "maria@email.com",
        "Rua A, 123",
        consentimentoLgpd);

    [Test]
    public async Task Should_CreatePatient_When_DataIsValidAndCpfIsUnique()
    {
        _patientRepository.Setup(r => r.CpfExistsAsync(It.IsAny<Guid>(), ValidCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.NomeCompleto, Is.EqualTo("Maria Silva"));
        Assert.That(result.Value.Cpf, Is.EqualTo(ValidCpf));

        _patientRepository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Regra: sem consentimento LGPD explícito, não cria ────────────────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_ConsentimentoLgpdIsFalse()
    {
        var command = ValidCommand(consentimentoLgpd: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.ConsentimentoLgpdObrigatorio"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Regra: CPF duplicado NO MESMO organization falha ───────────────────────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_CpfAlreadyExistsInSameOrganization()
    {
        var organizationId = Guid.NewGuid();

        _patientRepository.Setup(r => r.CpfExistsAsync(organizationId, ValidCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(ValidCommand(organizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.CpfJaCadastrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Regra: mesmo CPF em organization DIFERENTE não é erro — cada clínica é cliente independente ─
    [Test]
    public async Task Should_CreatePatient_When_CpfExistsButInADifferentOrganization()
    {
        var organizationB = Guid.NewGuid();

        // CpfExistsAsync é escopado por organization — pro organization B, o CPF não existe (mesmo que
        // exista no organization A, que não é consultado aqui).
        _patientRepository.Setup(r => r.CpfExistsAsync(organizationB, ValidCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(ValidCommand(organizationB), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _patientRepository.Verify(r => r.AddAsync(It.Is<Patient>(p => p.OrganizationId == organizationB), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Regressão: TOCTOU entre CpfExistsAsync e SaveChangesAsync ────────────────────────────
    [Test]
    public async Task Should_ReturnCpfJaCadastrado_When_UniqueConstraintViolationHappensOnSave()
    {
        _patientRepository.Setup(r => r.CpfExistsAsync(It.IsAny<Guid>(), ValidCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_Patients_OrganizationId_Cpf", new Exception("unique violation")));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.CpfJaCadastrado"));
    }
}
