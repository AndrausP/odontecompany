using Moq;
using Reporting.Application.Queries.GetReceitaPorProcedimento;
using Scheduling.Contracts;

namespace Reporting.UnitTests.Queries;

[TestFixture]
public class GetReceitaPorProcedimentoQueryHandlerTests
{
    private Mock<IReceitaPorProcedimentoProvider> _provider = null!;
    private GetReceitaPorProcedimentoQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _provider = new Mock<IReceitaPorProcedimentoProvider>();
        _handler = new GetReceitaPorProcedimentoQueryHandler(_provider.Object);
    }

    [Test]
    public async Task Should_ReturnListFromProvider_When_Called()
    {
        var organizationId = Guid.NewGuid();
        var dataInicio = new DateTime(2026, 1, 1);
        var dataFim = new DateTime(2026, 1, 31);
        var esperado = new List<ReceitaProcedimentoDto>
        {
            new(Guid.NewGuid(), "Limpeza", 5, 750m),
            new(null, "Sem procedimento", 2, 300m),
        };
        _provider
            .Setup(p => p.ObterAsync(organizationId, dataInicio, dataFim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var result = await _handler.Handle(new GetReceitaPorProcedimentoQuery(organizationId, dataInicio, dataFim), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(esperado));
    }

    [Test]
    public async Task Should_ReturnEmptyList_When_ProviderHasNoData()
    {
        var organizationId = Guid.NewGuid();
        var dataInicio = new DateTime(2026, 1, 1);
        var dataFim = new DateTime(2026, 1, 31);
        _provider
            .Setup(p => p.ObterAsync(organizationId, dataInicio, dataFim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReceitaProcedimentoDto>());

        var result = await _handler.Handle(new GetReceitaPorProcedimentoQuery(organizationId, dataInicio, dataFim), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }
}
