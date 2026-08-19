using FluentValidation;

namespace Reporting.Application.Queries.GetAgendaOcupacao;

public sealed class GetAgendaOcupacaoQueryValidator : AbstractValidator<GetAgendaOcupacaoQuery>
{
    public GetAgendaOcupacaoQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio);
    }
}
