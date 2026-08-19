using Identity.Application.Common;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetMe;

/// <summary>
/// Composição de 3 fontes (task 017): dados do usuário, memberships ATIVAS (cross-organization —
/// mesmo método já usado no login pra eleger a organization) com nome de organization resolvido em
/// lote, e convites pendentes (mesma composição de <c>GetMyInvitesQueryHandler</c>, via
/// <see cref="InvitePendingResolver"/> — evita duplicar a lógica entre os dois pontos de entrada).
/// Organizações a que o usuário NÃO pertence nunca aparecem: a fonte é sempre a lista de
/// memberships do PRÓPRIO usuário, nunca uma listagem geral de organizations.
/// </summary>
public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<MeResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IInviteRepository _inviteRepository;

    public GetMeQueryHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IOrganizationRepository organizationRepository,
        IInviteRepository inviteRepository)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _organizationRepository = organizationRepository;
        _inviteRepository = inviteRepository;
    }

    public async Task<Result<MeResultDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<MeResultDto>(DomainErrors.User.UsuarioInativo);

        var memberships = await _membershipRepository.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, cancellationToken);

        var organizations = await _organizationRepository.GetByIdsAsync(memberships.Select(m => m.OrganizationId).Distinct(), cancellationToken);
        var organizationNames = organizations.ToDictionary(o => o.Id, o => o.Nome);

        var organizationDtos = memberships
            .Select(m => new OrganizationMembershipDto(
                m.OrganizationId,
                organizationNames.TryGetValue(m.OrganizationId, out var nome) ? nome : "Organização removida",
                m.Role,
                m.Status,
                m.BranchId))
            .OrderBy(o => o.OrganizationName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pendingInvites = await InvitePendingResolver.ResolvePendingInvitesAsync(user.Email, _inviteRepository, _organizationRepository, cancellationToken);

        var userDto = new UserDto(user.Id, user.Nome, user.Email, user.Ativo);

        return Result.Success(new MeResultDto(userDto, organizationDtos, request.ActiveOrganizationId, pendingInvites));
    }
}
