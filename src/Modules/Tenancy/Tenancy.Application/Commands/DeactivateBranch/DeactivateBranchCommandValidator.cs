using FluentValidation;

namespace Tenancy.Application.Commands.DeactivateBranch;

public sealed class DeactivateBranchCommandValidator : AbstractValidator<DeactivateBranchCommand>
{
    public DeactivateBranchCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
