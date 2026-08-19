using Records.Domain.Entities;

namespace Records.Application.Interfaces;

/// <summary>
/// Leitura da trilha de auditoria — separada de <see cref="IAuditLogWriter"/> de propósito
/// (interface segregation: quem só precisa ler não depende do método de escrita, e vice-versa).
/// Só Admin consome isto (view de compliance), aplicado no controller via RBAC.
/// </summary>
public interface IAuditLogReader
{
    Task<(IReadOnlyList<RecordsAuditLog> Items, int TotalCount)> ListByProntuarioAsync(
        Guid prontuarioId, int page, int pageSize, CancellationToken ct = default);
}
