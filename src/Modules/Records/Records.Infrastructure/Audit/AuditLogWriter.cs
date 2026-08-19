using Microsoft.EntityFrameworkCore;
using Records.Application.Interfaces;
using Records.Domain.Entities;
using Records.Domain.Enums;
using Records.Infrastructure.Persistence;

namespace Records.Infrastructure.Audit;

/// <summary>
/// Implementa IAuditLogWriter e IAuditLogReader na mesma classe (leitura e escrita da mesma
/// tabela append-only, sem motivo pra dividir a implementação mesmo com as interfaces
/// segregadas). <see cref="LogAsync"/> chama <c>SaveChangesAsync</c> PRÓPRIO, separado do
/// SaveChanges da operação principal (prontuário/evolução/anexo) — cada chamada a SaveChanges
/// no EF Core é sua própria transação implícita quando não há <c>BeginTransaction</c> explícito
/// envolvendo as duas. Isso cumpre o requisito de auditoria "independente do fluxo normal de
/// dados": se algo falhar DEPOIS do log (ex: serialização da resposta HTTP), o rastro já está
/// persistido. Não expõe update/delete — só AddAsync via este método.
/// </summary>
public sealed class AuditLogWriter : IAuditLogWriter, IAuditLogReader
{
    private readonly RecordsDbContext _context;

    public AuditLogWriter(RecordsDbContext context) => _context = context;

    public async Task LogAsync(Guid organizationId, Guid prontuarioId, Guid pacienteId, Guid userId, AcaoAuditoria acao, CancellationToken ct = default)
    {
        var entry = RecordsAuditLog.Registrar(organizationId, prontuarioId, pacienteId, userId, acao);
        await _context.RecordsAuditLog.AddAsync(entry, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<RecordsAuditLog> Items, int TotalCount)> ListByProntuarioAsync(
        Guid prontuarioId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.RecordsAuditLog
            .Where(a => a.ProntuarioId == prontuarioId)
            .OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }
}
