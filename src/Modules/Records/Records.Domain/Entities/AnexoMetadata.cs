using Records.Domain.Errors;
using SharedKernel;

namespace Records.Domain.Entities;

/// <summary>
/// Metadados de um anexo (raio-x, exame) vinculado a um prontuário. O ARQUIVO em si vive em
/// object storage (<c>IObjectStorageService</c>); aqui só o metadado, em JSONB conceitualmente
/// simples o bastante pra mapear como entidade relacional direta. <see cref="CaminhoArmazenamento"/>
/// é a chave/path no storage, nunca o conteúdo binário. Imutável após criado — anexo errado se
/// resolve com novo upload, nunca edição do existente (mesma lógica de imutabilidade do histórico
/// clínico).
/// </summary>
public class AnexoMetadata : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid ProntuarioId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string NomeArquivo { get; private set; } = string.Empty;
    public string TipoConteudo { get; private set; } = string.Empty;
    public long TamanhoBytes { get; private set; }
    public string CaminhoArmazenamento { get; private set; } = string.Empty;
    public DateTime DataUpload { get; private set; }

    private AnexoMetadata() { } // EF Core

    private AnexoMetadata(
        Guid organizationId,
        Guid prontuarioId,
        Guid uploadedByUserId,
        string nomeArquivo,
        string tipoConteudo,
        long tamanhoBytes,
        string caminhoArmazenamento)
    {
        OrganizationId = organizationId;
        ProntuarioId = prontuarioId;
        UploadedByUserId = uploadedByUserId;
        NomeArquivo = nomeArquivo;
        TipoConteudo = tipoConteudo;
        TamanhoBytes = tamanhoBytes;
        CaminhoArmazenamento = caminhoArmazenamento;
        DataUpload = DateTime.UtcNow;
    }

    public const long TamanhoMaximoBytes = 20 * 1024 * 1024; // 20MB

    public static Result<AnexoMetadata> Create(
        Guid organizationId,
        Guid prontuarioId,
        Guid uploadedByUserId,
        string nomeArquivo,
        string tipoConteudo,
        long tamanhoBytes,
        string caminhoArmazenamento)
    {
        if (tamanhoBytes <= 0)
            return Result.Failure<AnexoMetadata>(DomainErrors.Anexo.ArquivoVazio);

        if (tamanhoBytes > TamanhoMaximoBytes)
            return Result.Failure<AnexoMetadata>(DomainErrors.Anexo.ArquivoMuitoGrande);

        return Result.Success(new AnexoMetadata(
            organizationId, prontuarioId, uploadedByUserId, nomeArquivo, tipoConteudo, tamanhoBytes, caminhoArmazenamento));
    }
}
