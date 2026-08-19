using Identity.Application.DTOs;
using Identity.Domain.Enums;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.CreateUser;

/// <summary>
/// OrganizationId nunca vem do corpo da requisição HTTP — o controller monta este command com o
/// organization_id extraído da claim do JWT do Admin autenticado, nunca de input do cliente.
/// </summary>
/// <summary>BranchId (Fase 5) é opcional — null = usuário sem restrição de branch (típico de Admin de rede).</summary>
public sealed record CreateUserCommand(Guid OrganizationId, string Nome, string Email, string Password, Role Role, Guid? BranchId = null)
    : IRequest<Result<CreateUserResultDto>>;
