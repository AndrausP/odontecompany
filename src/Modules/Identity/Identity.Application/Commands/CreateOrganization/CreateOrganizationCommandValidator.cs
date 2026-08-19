using FluentValidation;

namespace Identity.Application.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Usuário inválido.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome não pode ter mais de 150 caracteres.");
    }
}
