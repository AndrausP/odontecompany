using FluentValidation;

namespace Billing.Application.Commands.RegistrarPagamentoParcela;

public sealed class RegistrarPagamentoParcelaCommandValidator : AbstractValidator<RegistrarPagamentoParcelaCommand>
{
    public RegistrarPagamentoParcelaCommandValidator()
    {
        RuleFor(x => x.FaturaId).NotEmpty();
        RuleFor(x => x.ParcelaId).NotEmpty();
    }
}
