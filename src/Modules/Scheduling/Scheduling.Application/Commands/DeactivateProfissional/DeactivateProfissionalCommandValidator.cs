using FluentValidation;

namespace Scheduling.Application.Commands.DeactivateProfissional;

public sealed class DeactivateProfissionalCommandValidator : AbstractValidator<DeactivateProfissionalCommand>
{
    public DeactivateProfissionalCommandValidator()
    {
        RuleFor(x => x.ProfissionalId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
