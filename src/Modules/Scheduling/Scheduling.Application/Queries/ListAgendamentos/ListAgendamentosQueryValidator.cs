using FluentValidation;

namespace Scheduling.Application.Queries.ListAgendamentos;

public sealed class ListAgendamentosQueryValidator : AbstractValidator<ListAgendamentosQuery>
{
    public ListAgendamentosQueryValidator()
    {
        RuleFor(x => x.Pagination).NotNull().WithMessage("Paginação é obrigatória.");

        RuleFor(x => x.DataFim)
            .GreaterThanOrEqualTo(x => x.DataInicio!.Value)
            .When(x => x.DataInicio.HasValue && x.DataFim.HasValue)
            .WithMessage("Data fim não pode ser anterior à data início.");
    }
}
