using FluentValidation;

namespace Patients.Application.Queries.ListPatients;

public sealed class ListPatientsQueryValidator : AbstractValidator<ListPatientsQuery>
{
    public ListPatientsQueryValidator()
    {
        RuleFor(x => x.Pagination)
            .NotNull().WithMessage("Paginação é obrigatória.");
        // Page/PageSize não precisam de regra aqui: PageRequest já faz clamp (min 1, max 100)
        // no próprio init da propriedade — não existe valor inválido possível de chegar aqui.

        RuleFor(x => x.Nome)
            .MaximumLength(200).WithMessage("Filtro de nome não pode ter mais de 200 caracteres.");

        RuleFor(x => x.Cpf)
            .MaximumLength(14).WithMessage("Filtro de CPF não pode ter mais de 14 caracteres.");
    }
}
