using FluentValidation;

namespace Patients.Application.Commands.CreatePatient;

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization inválido.");

        RuleFor(x => x.NomeCompleto)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(200).WithMessage("Nome completo não pode ter mais de 200 caracteres.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório.")
            .Must(cpf => cpf.Count(char.IsDigit) == 11).WithMessage("CPF deve ter 11 dígitos.");
        // Validação do dígito verificador em si é responsabilidade do VO Cpf (Domain) — aqui só
        // barra input grosseiramente mal formado antes de chegar no handler.

        RuleFor(x => x.DataNascimento)
            .NotEqual(default(DateTime)).WithMessage("Data de nascimento é obrigatória.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow).WithMessage("Data de nascimento não pode ser no futuro.");

        RuleFor(x => x.Telefone)
            .NotEmpty().WithMessage("Telefone é obrigatório.")
            .MaximumLength(20).WithMessage("Telefone não pode ter mais de 20 caracteres.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email inválido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Endereco)
            .MaximumLength(500).WithMessage("Endereço não pode ter mais de 500 caracteres.");

        RuleFor(x => x.ConsentimentoLgpd)
            .Equal(true).WithMessage("Consentimento LGPD é obrigatório para cadastrar um paciente.");
    }
}
