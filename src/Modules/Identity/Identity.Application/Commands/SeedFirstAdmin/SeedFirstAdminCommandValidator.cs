using FluentValidation;

namespace Identity.Application.Commands.SeedFirstAdmin;

public sealed class SeedFirstAdminCommandValidator : AbstractValidator<SeedFirstAdminCommand>
{
    public SeedFirstAdminCommandValidator()
    {
        RuleFor(x => x.OrganizationNome)
            .NotEmpty().WithMessage("Nome do organization é obrigatório.");

        RuleFor(x => x.AdminNome)
            .NotEmpty().WithMessage("Nome do admin é obrigatório.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Email do admin é obrigatório.")
            .EmailAddress().WithMessage("Email do admin inválido.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Senha do admin é obrigatória.")
            .MinimumLength(8).WithMessage("Senha do admin deve ter ao menos 8 caracteres.");
    }
}
