using FluentValidation;

namespace Scheduling.Application.Commands.UpdateSala;

public sealed class UpdateSalaCommandValidator : AbstractValidator<UpdateSalaCommand>
{
    public UpdateSalaCommandValidator()
    {
        RuleFor(x => x.SalaId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CapacidadeMaxima).GreaterThan(0).When(x => x.CapacidadeMaxima is not null);
    }
}
