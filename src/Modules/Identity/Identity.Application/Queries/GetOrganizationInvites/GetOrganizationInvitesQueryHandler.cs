using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetOrganizationInvites;

/// <summary>Lista convites enviados pela organization (Owner/Admin) — task 016, item 3.</summary>
public sealed class GetOrganizationInvitesQueryHandler : IRequestHandler<GetOrganizationInvitesQuery, Result<List<OrganizationInviteDto>>>
{
    private readonly IInviteRepository _inviteRepository;

    public GetOrganizationInvitesQueryHandler(IInviteRepository inviteRepository) => _inviteRepository = inviteRepository;

    public async Task<Result<List<OrganizationInviteDto>>> Handle(GetOrganizationInvitesQuery request, CancellationToken cancellationToken)
    {
        var invites = await _inviteRepository.GetByOrganizationAsync(request.OrganizationId, cancellationToken);

        var dtos = invites
            .Select(i => new OrganizationInviteDto(i.Id, i.Email, i.Role, i.Status, i.ExpiresAt, i.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
