using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.CreateOrganization;

/// <summary>
/// UserId nunca vem do corpo da requisição — o controller monta a partir do sub do JWT do
/// usuário autenticado. Este endpoint TOLERA token sem organization (task 015, item 015): é
/// justamente a rota que dá a primeira organization a quem ainda não tem nenhuma.
/// </summary>
public sealed record CreateOrganizationCommand(Guid UserId, string Nome) : IRequest<Result<CreateOrganizationResultDto>>;
