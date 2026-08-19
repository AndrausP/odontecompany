using Billing.Contracts;
using Moq;
using Patients.Contracts;
using Reporting.Application.Queries.GetDashboardResumo;
using Scheduling.Contracts;

namespace Reporting.UnitTests.Queries;

[TestFixture]
public class GetDashboardResumoQueryHandlerTests
{
    private Mock<IPatientSummaryProvider> _patientSummaryProvider = null!;
    private Mock<IAgendaSummaryProvider> _agendaSummaryProvider = null!;
    private Mock<IFaturamentoSummaryProvider> _faturamentoSummaryProvider = null!;
    private GetDashboardResumoQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _patientSummaryProvider = new Mock<IPatientSummaryProvider>();
        _agendaSummaryProvider = new Mock<IAgendaSummaryProvider>();
        _faturamentoSummaryProvider = new Mock<IFaturamentoSummaryProvider>();
        _handler = new GetDashboardResumoQueryHandler(
            _patientSummaryProvider.Object, _agendaSummaryProvider.Object, _faturamentoSummaryProvider.Object);
    }

    [Test]
    public async Task Should_ComposeAllThreeProviders_When_DataIsAvailable()
    {
        var organizationId = Guid.NewGuid();
        var inicio = new DateTime(2026, 8, 1);
        var fim = new DateTime(2026, 8, 31);

        _patientSummaryProvider.Setup(p => p.ContarAtivosAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(42);
        _agendaSummaryProvider.Setup(a => a.ObterResumoAsync(organizationId, inicio, fim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendaResumoDto(TotalAgendamentos: 10, Agendados: 2, Confirmados: 3, Concluidos: 4, Cancelados: 1));
        _faturamentoSummaryProvider.Setup(f => f.ObterResumoAsync(organizationId, inicio, fim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FaturamentoResumoDto(ValorTotalFaturado: 1000m, ValorTotalRecebido: 600m, ValorTotalPendente: 400m, QuantidadeFaturas: 5));

        var result = await _handler.Handle(new GetDashboardResumoQuery(organizationId, inicio, fim), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.PacientesAtivos, Is.EqualTo(42));
        Assert.That(result.Value.TotalAgendamentos, Is.EqualTo(10));
        Assert.That(result.Value.AgendamentosConcluidos, Is.EqualTo(4));
        Assert.That(result.Value.ValorTotalFaturado, Is.EqualTo(1000m));
        Assert.That(result.Value.ValorTotalRecebido, Is.EqualTo(600m));
        Assert.That(result.Value.ValorTotalPendente, Is.EqualTo(400m));
        // 4 concluídos / 10 total = 40%
        Assert.That(result.Value.TaxaConclusao, Is.EqualTo(40.00m));
    }

    // ── Edge case: sem nenhum agendamento no período não pode dividir por zero ───────────────
    [Test]
    public async Task Should_ReturnZeroTaxaConclusao_When_NoAgendamentosInPeriod()
    {
        var organizationId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddDays(-30);
        var fim = DateTime.UtcNow;

        _patientSummaryProvider.Setup(p => p.ContarAtivosAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _agendaSummaryProvider.Setup(a => a.ObterResumoAsync(organizationId, inicio, fim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendaResumoDto(0, 0, 0, 0, 0));
        _faturamentoSummaryProvider.Setup(f => f.ObterResumoAsync(organizationId, inicio, fim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FaturamentoResumoDto(0m, 0m, 0m, 0));

        var result = await _handler.Handle(new GetDashboardResumoQuery(organizationId, inicio, fim), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.TaxaConclusao, Is.EqualTo(0m));
    }
}
