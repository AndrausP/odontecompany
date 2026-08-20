using Moq;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Queries.ListProfissionais;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.UnitTests.Queries;

[TestFixture]
public class ListProfissionaisQueryHandlerTests
{
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private ListProfissionaisQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _handler = new ListProfissionaisQueryHandler(_profissionalRepository.Object);
    }

    [Test]
    public async Task Should_ReturnMappedProfissionais_When_RepositoryHasData()
    {
        var profissional = Profissional.Criar(Guid.NewGuid(), "Dra. Ana", "Ortodontia", TipoContrato.Clt).Value;
        _profissionalRepository.Setup(r => r.ListAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Profissional> { profissional });

        var result = await _handler.Handle(new ListProfissionaisQuery(false), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value[0].Nome, Is.EqualTo("Dra. Ana"));
    }

    [Test]
    public async Task Should_PassIncludeInactiveThrough_When_Requested()
    {
        _profissionalRepository.Setup(r => r.ListAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Profissional>());

        await _handler.Handle(new ListProfissionaisQuery(true), CancellationToken.None);

        _profissionalRepository.Verify(r => r.ListAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
