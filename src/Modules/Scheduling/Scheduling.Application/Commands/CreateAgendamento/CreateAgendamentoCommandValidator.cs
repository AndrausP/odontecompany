using FluentValidation;

namespace Scheduling.Application.Commands.CreateAgendamento;

public sealed class CreateAgendamentoCommandValidator : AbstractValidator<CreateAgendamentoCommand>
{
    public CreateAgendamentoCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("Organization inválido.");
        RuleFor(x => x.PacienteId).NotEmpty().WithMessage("Paciente é obrigatório.");
        RuleFor(x => x.ProfissionalId).NotEmpty().WithMessage("Profissional é obrigatório.");
        RuleFor(x => x.SalaId).NotEmpty().WithMessage("Sala é obrigatória.");

        RuleFor(x => x.Inicio)
            .NotEqual(default(DateTime)).WithMessage("Início é obrigatório.");

        RuleFor(x => x.Fim)
            .GreaterThan(x => x.Inicio).WithMessage("Fim deve ser depois do início.");
        // Duração mínima (15min) e a checagem "início < fim" em si são validadas de novo pelo VO
        // PeriodoHorario (Domain) — aqui só barramos input grosseiramente mal formado antes do handler.
    }
}
