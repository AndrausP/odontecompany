using MediatR;
using SharedKernel;

namespace Tenancy.Application.Commands.DeactivateBranch;

/// <summary><paramref name="OrganizationId"/> vem do token (nunca da rota) — defesa contra IDOR,
/// mesmo gap fechado em `UpdateBranchCommand` (task 040, ver docs/decisions.md).</summary>
public sealed record DeactivateBranchCommand(Guid Id, Guid OrganizationId) : IRequest<Result>;
