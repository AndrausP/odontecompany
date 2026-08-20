using FluentValidation;

namespace Scheduling.Application.Commands.DeactivateSala;

public sealed class DeactivateSalaCommandValidator : AbstractValidator<DeactivateSalaCommand>
{
    public DeactivateSalaCommandValidator()
    {
        RuleFor(x => x.SalaId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
