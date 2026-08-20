using FluentValidation;

namespace Scheduling.Application.Commands.DeactivateProcedimento;

public sealed class DeactivateProcedimentoCommandValidator : AbstractValidator<DeactivateProcedimentoCommand>
{
    public DeactivateProcedimentoCommandValidator()
    {
        RuleFor(x => x.ProcedimentoId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
