using FluentValidation;

namespace Identity.Application.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome não pode ter mais de 150 caracteres.");

        // Mesma regra da criação (CreateOrganizationCommandValidator): sem máscara/regex, backend guarda cru.
        RuleFor(x => x.Cnpj).MaximumLength(18).When(x => x.Cnpj is not null);
        RuleFor(x => x.Telefone).MaximumLength(20).When(x => x.Telefone is not null);
        RuleFor(x => x.Endereco).MaximumLength(500).When(x => x.Endereco is not null);
    }
}
