using FluentValidation;

namespace Patients.Application.Commands.UpdatePatient;

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id do paciente é obrigatório.");

        RuleFor(x => x.NomeCompleto)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(200).WithMessage("Nome completo não pode ter mais de 200 caracteres.");

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
    }
}
