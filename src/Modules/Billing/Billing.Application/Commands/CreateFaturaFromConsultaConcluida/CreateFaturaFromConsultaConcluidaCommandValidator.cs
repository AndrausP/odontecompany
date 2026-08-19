using FluentValidation;

namespace Billing.Application.Commands.CreateFaturaFromConsultaConcluida;

public sealed class CreateFaturaFromConsultaConcluidaCommandValidator : AbstractValidator<CreateFaturaFromConsultaConcluidaCommand>
{
    public CreateFaturaFromConsultaConcluidaCommandValidator()
    {
        RuleFor(x => x.AgendamentoId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PacienteId).NotEmpty();
        RuleFor(x => x.ProfissionalId).NotEmpty();
        RuleFor(x => x.Valor).GreaterThan(0);
    }
}
