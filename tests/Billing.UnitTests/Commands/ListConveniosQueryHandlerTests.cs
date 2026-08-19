using Billing.Application.Interfaces;
using Billing.Application.Queries.ListConvenios;
using Billing.Domain.Entities;
using Moq;

namespace Billing.UnitTests.Commands;

[TestFixture]
public class ListConveniosQueryHandlerTests
{
    private Mock<IConvenioRepository> _convenioRepository = null!;
    private ListConveniosQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _convenioRepository = new Mock<IConvenioRepository>();
        _handler = new ListConveniosQueryHandler(_convenioRepository.Object);
    }

    [Test]
    public async Task Should_ReturnMappedConvenios_When_RepositoryHasData()
    {
        var convenio = Convenio.Create(Guid.NewGuid(), "Amil Dental", "AMIL-01").Value;
        _convenioRepository.Setup(r => r.ListAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Convenio> { convenio });

        var result = await _handler.Handle(new ListConveniosQuery(false), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value[0].Nome, Is.EqualTo("Amil Dental"));
    }
}
