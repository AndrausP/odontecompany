using Contracts.Abstractions.Pagination;
using MediatR;
using Records.Application.Interfaces;
using Records.Contracts;
using SharedKernel;

namespace Records.Application.Queries.ListAuditLog;

public sealed class ListAuditLogQueryHandler : IRequestHandler<ListAuditLogQuery, Result<PagedResult<AuditLogEntryDto>>>
{
    private readonly IAuditLogReader _auditLogReader;

    public ListAuditLogQueryHandler(IAuditLogReader auditLogReader) => _auditLogReader = auditLogReader;

    public async Task<Result<PagedResult<AuditLogEntryDto>>> Handle(ListAuditLogQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _auditLogReader.ListByProntuarioAsync(
            request.ProntuarioId, request.Pagination.Page, request.Pagination.PageSize, cancellationToken);

        var dtos = items
            .Select(a => new AuditLogEntryDto(a.Id, a.ProntuarioId, a.PacienteId, a.UserId, a.Acao.ToString(), a.Timestamp))
            .ToList();

        return Result.Success(PagedResult<AuditLogEntryDto>.Create(dtos, totalCount, request.Pagination.Page, request.Pagination.PageSize));
    }
}
