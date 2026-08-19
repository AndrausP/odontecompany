using FluentValidation;

namespace Billing.Application.Commands.CreateFaturaParticular;

public sealed class CreateFaturaParticularCommandValidator : AbstractValidator<CreateFaturaParticularCommand>
{
    public CreateFaturaParticularCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PacienteId).NotEmpty();
        RuleFor(x => x.ValorTotal).GreaterThan(0);
        RuleFor(x => x.NumeroParcelas).InclusiveBetween(1, 12);
    }
}
