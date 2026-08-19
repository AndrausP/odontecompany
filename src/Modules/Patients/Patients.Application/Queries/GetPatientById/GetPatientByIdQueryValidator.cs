using FluentValidation;

namespace Patients.Application.Queries.GetPatientById;

public sealed class GetPatientByIdQueryValidator : AbstractValidator<GetPatientByIdQuery>
{
    public GetPatientByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id do paciente é obrigatório.");
    }
}
