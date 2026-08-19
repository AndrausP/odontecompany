using Identity.Application.DTOs;
using Identity.Domain.Enums;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.CreateInvite;

/// <summary>
/// OrganizationId e InvitedByUserId nunca vêm do corpo da requisição — o controller monta a
/// partir da claim organization_id / sub do JWT do Owner/Admin autenticado (mesma regra de
/// CreateUserCommand: nunca confiar no cliente pra dizer em qual organization ele está agindo).
/// </summary>
public sealed record CreateInviteCommand(Guid OrganizationId, Guid InvitedByUserId, string Email, Role Role)
    : IRequest<Result<CreateInviteResultDto>>;
