using FluentValidation;

namespace Reporting.Application.Queries.GetDashboardResumo;

public sealed class GetDashboardResumoQueryValidator : AbstractValidator<GetDashboardResumoQuery>
{
    public GetDashboardResumoQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio)
            .WithMessage("DataFim deve ser maior ou igual a DataInicio.");
    }
}
