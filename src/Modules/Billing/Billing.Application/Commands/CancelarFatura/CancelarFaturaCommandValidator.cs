using FluentValidation;

namespace Billing.Application.Commands.CancelarFatura;

public sealed class CancelarFaturaCommandValidator : AbstractValidator<CancelarFaturaCommand>
{
    public CancelarFaturaCommandValidator()
    {
        RuleFor(x => x.FaturaId).NotEmpty();
    }
}
