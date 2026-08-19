using Moq;
using Patients.Application.Interfaces;
using Patients.Application.Queries.GetPatientById;
using Patients.Domain.Entities;

namespace Patients.UnitTests.Queries;

[TestFixture]
public class GetPatientByIdQueryHandlerTests
{
    private Mock<IPatientRepository> _patientRepository = null!;
    private GetPatientByIdQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _patientRepository = new Mock<IPatientRepository>();
        _handler = new GetPatientByIdQueryHandler(_patientRepository.Object);
    }

    [Test]
    public async Task Should_ReturnPatient_When_Found()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Maria Silva", "52998224725", new DateTime(1990, 5, 20), "11999999999", null, null, true).Value;
        _patientRepository.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var result = await _handler.Handle(new GetPatientByIdQuery(patient.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Id, Is.EqualTo(patient.Id));
    }

    [Test]
    public async Task Should_ReturnNotFound_When_PatientDoesNotExistOrBelongsToAnotherOrganization()
    {
        // GetByIdAsync já é escopado pelo filtro global de organization — paciente de outro organization
        // retorna null aqui do mesmo jeito que um id inexistente, sem distinção necessária.
        _patientRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        var result = await _handler.Handle(new GetPatientByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.NaoEncontrado"));
    }
}
