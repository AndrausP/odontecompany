using FluentValidation;

namespace Billing.Application.Commands.CreateConvenio;

public sealed class CreateConvenioCommandValidator : AbstractValidator<CreateConvenioCommand>
{
    public CreateConvenioCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
    }
}
