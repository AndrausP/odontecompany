using FluentValidation;

namespace Scheduling.Application.Commands.CreateSala;

public sealed class CreateSalaCommandValidator : AbstractValidator<CreateSalaCommand>
{
    public CreateSalaCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CapacidadeMaxima).GreaterThan(0).When(x => x.CapacidadeMaxima is not null);
    }
}
