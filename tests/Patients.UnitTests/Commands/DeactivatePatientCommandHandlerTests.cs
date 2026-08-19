using Moq;
using Patients.Application.Commands.DeactivatePatient;
using Patients.Application.Interfaces;
using Patients.Domain.Entities;

namespace Patients.UnitTests.Commands;

[TestFixture]
public class DeactivatePatientCommandHandlerTests
{
    private const string ValidCpf = "52998224725";
    private static readonly DateTime ValidDataNascimento = new(1990, 5, 20);

    private Mock<IPatientRepository> _patientRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private DeactivatePatientCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _patientRepository = new Mock<IPatientRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new DeactivatePatientCommandHandler(_patientRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_DeactivatePatient_When_PatientExists()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, true).Value;
        _patientRepository.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var result = await _handler.Handle(new DeactivatePatientCommand(patient.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(patient.Ativo, Is.False, "soft delete — Ativo deve virar false");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Nunca existe um método de repositório de remoção física sendo chamado — a única
        // interação com o repositório aqui é a leitura (GetByIdAsync); a "exclusão" é
        // inteiramente feita via SaveChangesAsync de uma alteração de estado (Ativo=false).
        _patientRepository.Verify(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()), Times.Once);
        _patientRepository.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Should_ReturnNotFound_When_PatientDoesNotExist()
    {
        _patientRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        var result = await _handler.Handle(new DeactivatePatientCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
