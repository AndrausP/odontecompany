using FluentValidation;
using Records.Domain.Entities;

namespace Records.Application.Commands.UploadAnexo;

public sealed class UploadAnexoCommandValidator : AbstractValidator<UploadAnexoCommand>
{
    public UploadAnexoCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ProntuarioId).NotEmpty();
        RuleFor(x => x.UploadedByUserId).NotEmpty();
        RuleFor(x => x.NomeArquivo).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TipoConteudo).NotEmpty();
        RuleFor(x => x.TamanhoBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(AnexoMetadata.TamanhoMaximoBytes);
    }
}
