using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetOrganizationInvites;

public sealed record GetOrganizationInvitesQuery(Guid OrganizationId) : IRequest<Result<List<OrganizationInviteDto>>>;
