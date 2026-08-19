using FluentValidation;

namespace Identity.Application.Queries.GetMe;

public sealed class GetMeQueryValidator : AbstractValidator<GetMeQuery>
{
    public GetMeQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Usuário inválido.");
    }
}
