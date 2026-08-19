using FluentValidation;

namespace Scheduling.Application.Queries.GetAgendamentoById;

public sealed class GetAgendamentoByIdQueryValidator : AbstractValidator<GetAgendamentoByIdQuery>
{
    public GetAgendamentoByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id do agendamento é obrigatório.");
    }
}
