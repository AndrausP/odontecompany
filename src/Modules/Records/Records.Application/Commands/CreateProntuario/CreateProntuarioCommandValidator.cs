using FluentValidation;

namespace Records.Application.Commands.CreateProntuario;

public sealed class CreateProntuarioCommandValidator : AbstractValidator<CreateProntuarioCommand>
{
    public CreateProntuarioCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PacienteId).NotEmpty();
        RuleFor(x => x.CriadoPorUserId).NotEmpty();
    }
}
