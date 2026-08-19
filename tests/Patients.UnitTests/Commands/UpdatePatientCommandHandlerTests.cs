using Moq;
using Patients.Application.Commands.UpdatePatient;
using Patients.Application.Interfaces;
using Patients.Domain.Entities;

namespace Patients.UnitTests.Commands;

[TestFixture]
public class UpdatePatientCommandHandlerTests
{
    private const string ValidCpf = "52998224725";
    private static readonly DateTime ValidDataNascimento = new(1990, 5, 20);

    private Mock<IPatientRepository> _patientRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UpdatePatientCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _patientRepository = new Mock<IPatientRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdatePatientCommandHandler(_patientRepository.Object, _unitOfWork.Object);
    }

    private static Patient CreatePatient() => Patient.Create(
        Guid.NewGuid(), "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: true).Value;

    [Test]
    public async Task Should_UpdatePatient_When_PatientExists()
    {
        var patient = CreatePatient();
        _patientRepository.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var command = new UpdatePatientCommand(patient.Id, "Maria Santos", ValidDataNascimento, "11988888888", "novo@email.com", "Rua nova");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.NomeCompleto, Is.EqualTo("Maria Santos"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNotFound_When_PatientDoesNotExist()
    {
        _patientRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        var command = new UpdatePatientCommand(Guid.NewGuid(), "Maria Santos", ValidDataNascimento, "11988888888", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Regra: CPF é imutável — UpdatePatientCommand nem sequer tem campo Cpf ────────────────
    [Test]
    public async Task Should_KeepOriginalCpf_When_PatientIsUpdated()
    {
        var patient = CreatePatient();
        var originalCpf = patient.Cpf;
        _patientRepository.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var command = new UpdatePatientCommand(patient.Id, "Outro Nome", ValidDataNascimento, "11977777777", null, null);
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(patient.Cpf, Is.EqualTo(originalCpf));
    }
}
