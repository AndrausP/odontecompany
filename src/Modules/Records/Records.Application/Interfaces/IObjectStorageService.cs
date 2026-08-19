namespace Records.Application.Interfaces;

/// <summary>
/// Porta de armazenamento de arquivo binário (anexos de prontuário — raio-x, exames). Só
/// metadado (nome, tipo, tamanho, caminho) mora no banco relacional; o conteúdo binário vive
/// aqui. Implementação de dev (<c>Records.Infrastructure.Storage.LocalDiskObjectStorageService</c>)
/// grava em disco local — produção precisa de object storage real (S3/Azure Blob), troca de
/// implementação sem tocar em Application (ports-and-adapters).
/// </summary>
public interface IObjectStorageService
{
    /// <summary>Salva o conteúdo e devolve o caminho/chave de armazenamento (nunca o binário de volta pra Application).</summary>
    Task<string> SaveAsync(Guid organizationId, Guid prontuarioId, string nomeArquivo, Stream content, CancellationToken ct = default);
}
