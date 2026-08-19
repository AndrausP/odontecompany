using FluentValidation;

namespace Scheduling.Application.Commands.CancelarAgendamento;

public sealed class CancelarAgendamentoCommandValidator : AbstractValidator<CancelarAgendamentoCommand>
{
    public CancelarAgendamentoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id do agendamento é obrigatório.");
        RuleFor(x => x.Motivo).MaximumLength(500).WithMessage("Motivo não pode ter mais de 500 caracteres.");
    }
}
