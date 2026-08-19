using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetMe;

/// <summary>
/// UserId nunca vem do corpo/query — sempre do sub do JWT. ActiveOrganizationId vem da claim
/// organization_id do MESMO token (pode ser <c>null</c> — usuário sem organization ativa, um dos
/// 4 endpoints que tolera isso). Nunca resolvido a partir do banco: é o estado do TOKEN atual,
/// não uma preferência persistida.
/// </summary>
public sealed record GetMeQuery(Guid UserId, Guid? ActiveOrganizationId) : IRequest<Result<MeResultDto>>;
