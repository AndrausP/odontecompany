using MediatR;
using Records.Application.Interfaces;
using Records.Application.Mapping;
using Records.Contracts;
using Records.Domain.Entities;
using Records.Domain.Enums;
using Records.Domain.Errors;
using SharedKernel;

namespace Records.Application.Commands.UploadAnexo;

public sealed class UploadAnexoCommandHandler : IRequestHandler<UploadAnexoCommand, Result<AnexoDto>>
{
    private readonly IProntuarioRepository _prontuarioRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IUnitOfWork _unitOfWork;

    public UploadAnexoCommandHandler(
        IProntuarioRepository prontuarioRepository,
        IObjectStorageService objectStorageService,
        IAuditLogWriter auditLogWriter,
        IUnitOfWork unitOfWork)
    {
        _prontuarioRepository = prontuarioRepository;
        _objectStorageService = objectStorageService;
        _auditLogWriter = auditLogWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AnexoDto>> Handle(UploadAnexoCommand request, CancellationToken cancellationToken)
    {
        var prontuario = await _prontuarioRepository.GetByIdAsync(request.ProntuarioId, cancellationToken);
        if (prontuario is null)
            return Result.Failure<AnexoDto>(DomainErrors.Prontuario.NaoEncontrado);

        if (!prontuario.Ativo)
            return Result.Failure<AnexoDto>(DomainErrors.Prontuario.Inativo);

        // Salva o binário no object storage ANTES de criar o metadado — se a criação do
        // metadado falhar depois, sobra um arquivo órfão no storage (aceitável, é mais seguro
        // que o inverso: metadado apontando pra um arquivo que nunca chegou a ser gravado).
        var caminhoArmazenamento = await _objectStorageService.SaveAsync(
            request.OrganizationId, prontuario.Id, request.NomeArquivo, request.Content, cancellationToken);

        var anexoResult = AnexoMetadata.Create(
            request.OrganizationId, prontuario.Id, request.UploadedByUserId,
            request.NomeArquivo, request.TipoConteudo, request.TamanhoBytes, caminhoArmazenamento);

        if (anexoResult.IsFailure)
            return Result.Failure<AnexoDto>(anexoResult.Error);

        var anexo = anexoResult.Value;

        await _prontuarioRepository.AddAnexoAsync(anexo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogWriter.LogAsync(
            request.OrganizationId, prontuario.Id, prontuario.PacienteId, request.UploadedByUserId,
            AcaoAuditoria.UploadAnexo, cancellationToken);

        return Result.Success(anexo.ToDto());
    }
}
