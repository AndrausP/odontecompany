using FluentValidation;

namespace Reporting.Application.Queries.GetFaturamentoPorPeriodo;

public sealed class GetFaturamentoPorPeriodoQueryValidator : AbstractValidator<GetFaturamentoPorPeriodoQuery>
{
    public GetFaturamentoPorPeriodoQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio);
    }
}
