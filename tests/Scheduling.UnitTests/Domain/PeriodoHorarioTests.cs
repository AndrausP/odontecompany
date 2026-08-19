using Scheduling.Domain.ValueObjects;

namespace Scheduling.UnitTests.Domain;

[TestFixture]
public class PeriodoHorarioTests
{
    private static readonly DateTime BaseInicio = new(2026, 8, 20, 9, 0, 0);

    [Test]
    public void Should_CreatePeriodo_When_DuracaoIsExactly15Minutes()
    {
        var result = PeriodoHorario.Create(BaseInicio, BaseInicio.AddMinutes(15));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Inicio, Is.EqualTo(BaseInicio));
        Assert.That(result.Value.Fim, Is.EqualTo(BaseInicio.AddMinutes(15)));
    }

    [Test]
    public void Should_ReturnFailure_When_InicioIsEqualToFim()
    {
        var result = PeriodoHorario.Create(BaseInicio, BaseInicio);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("PeriodoHorario.InicioDeveSerAntesDoFim"));
    }

    [Test]
    public void Should_ReturnFailure_When_InicioIsAfterFim()
    {
        var result = PeriodoHorario.Create(BaseInicio, BaseInicio.AddMinutes(-10));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("PeriodoHorario.InicioDeveSerAntesDoFim"));
    }

    [Test]
    public void Should_ReturnFailure_When_DuracaoIsLessThan15Minutes()
    {
        var result = PeriodoHorario.Create(BaseInicio, BaseInicio.AddMinutes(14));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("PeriodoHorario.DuracaoMinimaNaoAtingida"));
    }

    [Test]
    public void Should_DetectOverlap_When_PeriodsIntersect()
    {
        var periodoA = PeriodoHorario.Create(BaseInicio, BaseInicio.AddMinutes(30)).Value;
        var periodoB = PeriodoHorario.Create(BaseInicio.AddMinutes(15), BaseInicio.AddMinutes(45)).Value;

        Assert.That(periodoA.Sobrepoe(periodoB), Is.True);
        Assert.That(periodoB.Sobrepoe(periodoA), Is.True);
    }

    [Test]
    public void Should_NotDetectOverlap_When_PeriodsOnlyTouchAtTheBorder()
    {
        var periodoA = PeriodoHorario.Create(BaseInicio, BaseInicio.AddMinutes(30)).Value;
        var periodoB = PeriodoHorario.Create(BaseInicio.AddMinutes(30), BaseInicio.AddMinutes(60)).Value;

        Assert.That(periodoA.Sobrepoe(periodoB), Is.False, "intervalos semi-abertos [Inicio, Fim) — tocar na borda não é sobreposição");
    }

    [Test]
    public void Should_NotDetectOverlap_When_PeriodsAreFarApart()
    {
        var periodoA = PeriodoHorario.Create(BaseInicio, BaseInicio.AddMinutes(30)).Value;
        var periodoB = PeriodoHorario.Create(BaseInicio.AddHours(2), BaseInicio.AddHours(2).AddMinutes(30)).Value;

        Assert.That(periodoA.Sobrepoe(periodoB), Is.False);
    }
}
