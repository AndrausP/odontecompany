using FluentValidation;

namespace Scheduling.Application.Commands.CreateProfissional;

public sealed class CreateProfissionalCommandValidator : AbstractValidator<CreateProfissionalCommand>
{
    public CreateProfissionalCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Especialidade).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TipoContrato).IsInEnum();
        RuleFor(x => x.PercentualComissaoDefault).InclusiveBetween(0, 100).When(x => x.PercentualComissaoDefault is not null);
        RuleFor(x => x.Email).EmailAddress().WithMessage("Email inválido.").When(x => x.Email is not null);
    }
}
