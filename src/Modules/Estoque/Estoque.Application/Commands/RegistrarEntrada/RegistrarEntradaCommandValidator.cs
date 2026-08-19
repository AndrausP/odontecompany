using FluentValidation;

namespace Estoque.Application.Commands.RegistrarEntrada;

public sealed class RegistrarEntradaCommandValidator : AbstractValidator<RegistrarEntradaCommand>
{
    public RegistrarEntradaCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
