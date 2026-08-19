using Identity.Application.Common;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetMyInvites;

public sealed class GetMyInvitesQueryHandler : IRequestHandler<GetMyInvitesQuery, Result<List<PendingInviteDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public GetMyInvitesQueryHandler(
        IUserRepository userRepository,
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository)
    {
        _userRepository = userRepository;
        _inviteRepository = inviteRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<Result<List<PendingInviteDto>>> Handle(GetMyInvitesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<List<PendingInviteDto>>(DomainErrors.User.UsuarioInativo);

        var dtos = await InvitePendingResolver.ResolvePendingInvitesAsync(user.Email, _inviteRepository, _organizationRepository, cancellationToken);

        return Result.Success(dtos);
    }
}
