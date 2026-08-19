using FluentValidation;

namespace Identity.Application.Commands.SwitchOrganization;

public sealed class SwitchOrganizationCommandValidator : AbstractValidator<SwitchOrganizationCommand>
{
    public SwitchOrganizationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Usuário inválido.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization inválido.");
    }
}
