using FluentValidation;

namespace Tenancy.Application.Commands.UpdateBranch;

public sealed class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Telefone).MaximumLength(20).When(x => x.Telefone is not null);
        RuleFor(x => x.Endereco).MaximumLength(500).When(x => x.Endereco is not null);
    }
}
