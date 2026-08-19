using FluentValidation;

namespace Identity.Application.Commands.CreateInvite;

public sealed class CreateInviteCommandValidator : AbstractValidator<CreateInviteCommand>
{
    public CreateInviteCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization inválido.");

        RuleFor(x => x.InvitedByUserId)
            .NotEmpty().WithMessage("Usuário inválido.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Papel inválido.");
    }
}
