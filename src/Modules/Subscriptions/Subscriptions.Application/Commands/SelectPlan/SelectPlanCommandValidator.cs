using FluentValidation;

namespace Subscriptions.Application.Commands.SelectPlan;

public sealed class SelectPlanCommandValidator : AbstractValidator<SelectPlanCommand>
{
    public SelectPlanCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Tier).IsInEnum();
    }
}
