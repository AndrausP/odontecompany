using FluentValidation;

namespace Billing.Application.Commands.CreateFaturaConvenio;

public sealed class CreateFaturaConvenioCommandValidator : AbstractValidator<CreateFaturaConvenioCommand>
{
    public CreateFaturaConvenioCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PacienteId).NotEmpty();
        RuleFor(x => x.ConvenioId).NotEmpty();
        RuleFor(x => x.ValorTotal).GreaterThan(0);
    }
}
