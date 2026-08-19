using FluentValidation;

namespace Records.Application.Commands.AddEvolucaoClinica;

public sealed class AddEvolucaoClinicaCommandValidator : AbstractValidator<AddEvolucaoClinicaCommand>
{
    public AddEvolucaoClinicaCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ProntuarioId).NotEmpty();
        RuleFor(x => x.ProfissionalUserId).NotEmpty();
        RuleFor(x => x.DescricaoClinica).NotEmpty().MaximumLength(8000);
    }
}
