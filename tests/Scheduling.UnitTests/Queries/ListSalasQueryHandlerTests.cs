using Moq;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Queries.ListSalas;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Queries;

[TestFixture]
public class ListSalasQueryHandlerTests
{
    private Mock<ISalaRepository> _salaRepository = null!;
    private ListSalasQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _salaRepository = new Mock<ISalaRepository>();
        _handler = new ListSalasQueryHandler(_salaRepository.Object);
    }

    [Test]
    public async Task Should_ReturnMappedSalas_When_RepositoryHasData()
    {
        var sala = Sala.Criar(Guid.NewGuid(), "Sala 1").Value;
        _salaRepository.Setup(r => r.ListAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Sala> { sala });

        var result = await _handler.Handle(new ListSalasQuery(false), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value[0].Nome, Is.EqualTo("Sala 1"));
    }
}
