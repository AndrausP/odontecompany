using FluentValidation;

namespace Scheduling.Application.Commands.MarcarAgendamentoConcluido;

public sealed class MarcarAgendamentoConcluidoCommandValidator : AbstractValidator<MarcarAgendamentoConcluidoCommand>
{
    public MarcarAgendamentoConcluidoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id do agendamento é obrigatório.");
        RuleFor(x => x.Valor).GreaterThan(0).WithMessage("Valor da consulta deve ser maior que zero.");
    }
}
