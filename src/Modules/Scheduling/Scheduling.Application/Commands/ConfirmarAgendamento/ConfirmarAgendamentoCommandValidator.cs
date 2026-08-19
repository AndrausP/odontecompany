using FluentValidation;

namespace Scheduling.Application.Commands.ConfirmarAgendamento;

public sealed class ConfirmarAgendamentoCommandValidator : AbstractValidator<ConfirmarAgendamentoCommand>
{
    public ConfirmarAgendamentoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id do agendamento é obrigatório.");
    }
}
