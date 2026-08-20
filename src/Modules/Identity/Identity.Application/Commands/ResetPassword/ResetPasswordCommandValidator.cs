using FluentValidation;

namespace Identity.Application.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token é obrigatório.");

        // Mesma regra de senha do Signup (SignupCommandValidator) — 8 caracteres mínimo.
        RuleFor(x => x.NovaSenha).MinimumLength(8).WithMessage("Senha deve ter ao menos 8 caracteres.");
    }
}
