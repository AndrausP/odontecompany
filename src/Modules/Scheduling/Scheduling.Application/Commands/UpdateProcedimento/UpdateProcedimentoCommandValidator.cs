using FluentValidation;

namespace Scheduling.Application.Commands.UpdateProcedimento;

public sealed class UpdateProcedimentoCommandValidator : AbstractValidator<UpdateProcedimentoCommand>
{
    public UpdateProcedimentoCommandValidator()
    {
        RuleFor(x => x.ProcedimentoId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ValorPadrao).GreaterThanOrEqualTo(0).When(x => x.ValorPadrao is not null);
        RuleFor(x => x.DuracaoPadraoMinutos).GreaterThan(0).When(x => x.DuracaoPadraoMinutos is not null);
    }
}
