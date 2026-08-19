using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.SwitchOrganization;

/// <summary>
/// UserId NUNCA vem do corpo da requisição — o controller monta este command com o sub extraído
/// do JWT do usuário autenticado (mesmo padrão de <c>CreateUserCommand.OrganizationId</c>), nunca
/// de input do cliente.
/// </summary>
public sealed record SwitchOrganizationCommand(Guid UserId, Guid OrganizationId) : IRequest<Result<LoginResultDto>>;
