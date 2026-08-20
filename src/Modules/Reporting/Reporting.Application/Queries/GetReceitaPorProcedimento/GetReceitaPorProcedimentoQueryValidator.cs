using FluentValidation;

namespace Reporting.Application.Queries.GetReceitaPorProcedimento;

public sealed class GetReceitaPorProcedimentoQueryValidator : AbstractValidator<GetReceitaPorProcedimentoQuery>
{
    public GetReceitaPorProcedimentoQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio);
    }
}
