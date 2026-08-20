using Identity.Application.Interfaces;
using Identity.Application.Queries.GetOrganization;
using Identity.Domain.Entities;
using Moq;

namespace Identity.UnitTests.Queries;

[TestFixture]
public class GetOrganizationQueryHandlerTests
{
    private Mock<IOrganizationRepository> _organizationRepository = null!;
    private GetOrganizationQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _organizationRepository = new Mock<IOrganizationRepository>();
        _handler = new GetOrganizationQueryHandler(_organizationRepository.Object);
    }

    [Test]
    public async Task Should_ReturnDto_When_OrganizationExists()
    {
        var organization = Organization.Create("Clínica Sorriso", "00.000.000/0001-00", "11999999999", "Rua A, 1").Value;
        _organizationRepository.Setup(r => r.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);

        var result = await _handler.Handle(new GetOrganizationQuery(organization.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nome, Is.EqualTo("Clínica Sorriso"));
        Assert.That(result.Value.Cnpj, Is.EqualTo("00.000.000/0001-00"));
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_OrganizationDoesNotExist()
    {
        _organizationRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var result = await _handler.Handle(new GetOrganizationQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Organization.NaoEncontrado"));
    }
}
