using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

/// <summary>
/// Convite pendente do ponto de vista do CONVIDADO — cross-organization (task 016/017). Usado
/// tanto por <c>GET /api/me/invites</c> quanto embutido em <c>GET /api/me</c>.
/// </summary>
public sealed record PendingInviteDto(Guid InviteId, Guid OrganizationId, string OrganizationName, Role Role, DateTime ExpiresAt);
