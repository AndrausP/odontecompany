using FluentValidation;

namespace Scheduling.Application.Queries.ListarDisponibilidade;

public sealed class ListarDisponibilidadeQueryValidator : AbstractValidator<ListarDisponibilidadeQuery>
{
    public ListarDisponibilidadeQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("Organization inválido.");
        RuleFor(x => x.ProfissionalId).NotEmpty().WithMessage("Profissional é obrigatório.");
        RuleFor(x => x.Data).NotEqual(default(DateOnly)).WithMessage("Data é obrigatória.");
    }
}
