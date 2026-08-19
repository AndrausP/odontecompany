using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

/// <summary>Convite listado do ponto de vista da organization (Owner/Admin gerenciando convites enviados) — task 016.</summary>
public sealed record OrganizationInviteDto(Guid InviteId, string Email, Role Role, InviteStatus Status, DateTime ExpiresAt, DateTime CreatedAt);
