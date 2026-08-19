using FluentValidation;

namespace Estoque.Application.Commands.CreateItemEstoque;

public sealed class CreateItemEstoqueCommandValidator : AbstractValidator<CreateItemEstoqueCommand>
{
    public CreateItemEstoqueCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnidadeMedida).NotEmpty().MaximumLength(20);
        RuleFor(x => x.QuantidadeMinima).GreaterThanOrEqualTo(0);
    }
}
