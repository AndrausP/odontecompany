using FluentValidation;

namespace Patients.Application.Commands.DeactivatePatient;

public sealed class DeactivatePatientCommandValidator : AbstractValidator<DeactivatePatientCommand>
{
    public DeactivatePatientCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id do paciente é obrigatório.");
    }
}
