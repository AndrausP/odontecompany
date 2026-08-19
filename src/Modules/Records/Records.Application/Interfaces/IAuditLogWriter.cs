using Records.Domain.Enums;

namespace Records.Application.Interfaces;

/// <summary>
/// Grava entrada na trilha de auditoria (append-only) em transação PRÓPRIA, independente da
/// operação principal (leitura ou escrita de prontuário) — ver <c>RecordsAuditLog</c> pro porquê.
/// Não expõe update/delete de propósito: não existe caminho de código pra alterar/apagar uma
/// entrada já gravada.
/// </summary>
public interface IAuditLogWriter
{
    Task LogAsync(Guid organizationId, Guid prontuarioId, Guid pacienteId, Guid userId, AcaoAuditoria acao, CancellationToken ct = default);
}
