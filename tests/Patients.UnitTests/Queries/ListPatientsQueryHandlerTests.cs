using Contracts.Abstractions.Pagination;
using Moq;
using Patients.Application.Interfaces;
using Patients.Application.Queries.ListPatients;
using Patients.Domain.Entities;

namespace Patients.UnitTests.Queries;

[TestFixture]
public class ListPatientsQueryHandlerTests
{
    private Mock<IPatientRepository> _patientRepository = null!;
    private ListPatientsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _patientRepository = new Mock<IPatientRepository>();
        _handler = new ListPatientsQueryHandler(_patientRepository.Object);
    }

    [Test]
    public async Task Should_ReturnPagedResult_When_RepositoryReturnsItems()
    {
        var patients = new List<Patient>
        {
            Patient.Create(Guid.NewGuid(), "Ana", "52998224725", new DateTime(1990, 1, 1), "11999999999", null, null, true).Value
        };

        _patientRepository
            .Setup(r => r.ListAsync(null, null, false, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((patients, 1));

        var query = new ListPatientsQuery(new PageRequest { Page = 1, PageSize = 20 });
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Items, Has.Count.EqualTo(1));
        Assert.That(result.Value.TotalCount, Is.EqualTo(1));
        Assert.That(result.Value.Page, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_PassFiltersThrough_When_NomeCpfAndIncludeInactiveAreProvided()
    {
        _patientRepository
            .Setup(r => r.ListAsync("Ana", "52998224725", true, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Patient>(), 0));

        var query = new ListPatientsQuery(new PageRequest { Page = 2, PageSize = 10 }, "Ana", "52998224725", IncludeInactive: true);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _patientRepository.Verify(r => r.ListAsync("Ana", "52998224725", true, 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnEmptyPagedResult_When_NoPatientsMatch()
    {
        _patientRepository
            .Setup(r => r.ListAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Patient>(), 0));

        var query = new ListPatientsQuery(new PageRequest());
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Items, Is.Empty);
        Assert.That(result.Value.TotalCount, Is.EqualTo(0));
    }
}
