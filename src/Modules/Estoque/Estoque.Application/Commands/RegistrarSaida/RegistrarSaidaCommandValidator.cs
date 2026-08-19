using FluentValidation;

namespace Estoque.Application.Commands.RegistrarSaida;

public sealed class RegistrarSaidaCommandValidator : AbstractValidator<RegistrarSaidaCommand>
{
    public RegistrarSaidaCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
