using FluentValidation;

namespace Reporting.Application.Queries.GetComissoesPorPeriodo;

public sealed class GetComissoesPorPeriodoQueryValidator : AbstractValidator<GetComissoesPorPeriodoQuery>
{
    public GetComissoesPorPeriodoQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio)
            .WithMessage("Data fim não pode ser anterior à data início.");
    }
}
