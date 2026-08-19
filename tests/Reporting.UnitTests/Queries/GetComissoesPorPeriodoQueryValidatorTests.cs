using Reporting.Application.Queries.GetComissoesPorPeriodo;

namespace Reporting.UnitTests.Queries;

[TestFixture]
public class GetComissoesPorPeriodoQueryValidatorTests
{
    private GetComissoesPorPeriodoQueryValidator _validator = null!;

    [SetUp]
    public void Setup() => _validator = new GetComissoesPorPeriodoQueryValidator();

    [Test]
    public void Should_Fail_When_DataFimIsBeforeDataInicio()
    {
        var query = new GetComissoesPorPeriodoQuery(
            Guid.NewGuid(),
            DataInicio: new DateTime(2026, 8, 10),
            DataFim: new DateTime(2026, 8, 1),
            BranchId: null,
            Classe: null,
            ProfissionalId: null);

        var result = _validator.Validate(query);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Select(e => e.ErrorMessage),
            Has.Some.EqualTo("Data fim não pode ser anterior à data início."));
    }

    [Test]
    public void Should_Pass_When_DataFimEqualsDataInicio()
    {
        var dia = new DateTime(2026, 8, 10);
        var query = new GetComissoesPorPeriodoQuery(Guid.NewGuid(), dia, dia, null, null, null);

        var result = _validator.Validate(query);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Should_Fail_When_OrganizationIdIsEmpty()
    {
        var query = new GetComissoesPorPeriodoQuery(
            Guid.Empty, new DateTime(2026, 8, 1), new DateTime(2026, 8, 10), null, null, null);

        var result = _validator.Validate(query);

        Assert.That(result.IsValid, Is.False);
    }
}
