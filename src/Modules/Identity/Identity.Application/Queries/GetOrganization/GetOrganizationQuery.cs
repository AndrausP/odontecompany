using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetOrganization;

/// <summary>Busca os dados editáveis da organização — tela de configurações.</summary>
public sealed record GetOrganizationQuery(Guid OrganizationId) : IRequest<Result<OrganizationDto>>;
