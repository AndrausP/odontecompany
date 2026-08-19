using FluentValidation;

namespace Identity.Application.Commands.AcceptInvite;

public sealed class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Usuário inválido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório.");
    }
}
