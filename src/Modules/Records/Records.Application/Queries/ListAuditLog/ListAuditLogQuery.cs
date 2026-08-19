using Contracts.Abstractions.Pagination;
using MediatR;
using Records.Contracts;
using SharedKernel;

namespace Records.Application.Queries.ListAuditLog;

/// <summary>Admin-only — aplicado via RBAC no controller, não repetido aqui de propósito (Application não conhece papéis).</summary>
public sealed record ListAuditLogQuery(Guid ProntuarioId, PageRequest Pagination) : IRequest<Result<PagedResult<AuditLogEntryDto>>>;
