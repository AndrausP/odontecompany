using FluentValidation;

namespace Scheduling.Application.Commands.LinkProfissionalUser;

public sealed class LinkProfissionalUserCommandValidator : AbstractValidator<LinkProfissionalUserCommand>
{
    public LinkProfissionalUserCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
