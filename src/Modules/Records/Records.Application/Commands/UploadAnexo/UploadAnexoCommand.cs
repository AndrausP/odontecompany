using MediatR;
using Records.Contracts;
using SharedKernel;

namespace Records.Application.Commands.UploadAnexo;

/// <summary>
/// <see cref="Content"/> é lido uma única vez pelo handler (repassado direto pro
/// <c>IObjectStorageService</c>, nunca bufferizado inteiro em memória na Application).
/// <see cref="TamanhoBytes"/> vem do <c>IFormFile.Length</c> no controller — validado ANTES de
/// tentar salvar o stream, pra rejeitar arquivo grande demais sem gravar nada em disco.
/// </summary>
public sealed record UploadAnexoCommand(
    Guid OrganizationId,
    Guid ProntuarioId,
    Guid UploadedByUserId,
    string NomeArquivo,
    string TipoConteudo,
    long TamanhoBytes,
    Stream Content
) : IRequest<Result<AnexoDto>>;
