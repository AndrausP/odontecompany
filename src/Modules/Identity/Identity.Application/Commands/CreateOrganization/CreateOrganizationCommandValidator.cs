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

        // Cnpj/Telefone/Endereco são opcionais (sprint-11) — só valida tamanho quando informado, sem checar formato (sem máscara/regex; frontend formata visualmente, backend guarda cru).
        RuleFor(x => x.Cnpj).MaximumLength(18).When(x => x.Cnpj is not null);
        RuleFor(x => x.Telefone).MaximumLength(20).When(x => x.Telefone is not null);
        RuleFor(x => x.Endereco).MaximumLength(500).When(x => x.Endereco is not null);
    }
}
