using FluentValidation;

namespace Scheduling.Application.Commands.CreateProfissional;

public sealed class CreateProfissionalCommandValidator : AbstractValidator<CreateProfissionalCommand>
{
    public CreateProfissionalCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Especialidade).NotEmpty().MaximumLength(200);
    }
}
