using Identity.Application.DTOs;
using Identity.Application.Interfaces;

namespace Identity.Application.Common;

/// <summary>
/// Composição repetida por dois pontos de entrada (<c>GetMyInvitesQueryHandler</c> — task 016 —
/// e <c>GetMeQueryHandler</c> — task 017): busca convites pendentes cross-organization pro email
/// do usuário e resolve o NOME de cada organization em lote (<see cref="IOrganizationRepository.GetByIdsAsync"/>),
/// evitando N idas ao banco (uma por convite). Extraído aqui pra não duplicar a composição entre
/// os dois handlers.
/// </summary>
internal static class InvitePendingResolver
{
    public static async Task<List<PendingInviteDto>> ResolvePendingInvitesAsync(
        string email,
        IInviteRepository inviteRepository,
        IOrganizationRepository organizationRepository,
        CancellationToken ct)
    {
        var invites = await inviteRepository.GetPendingByEmailAcrossOrganizationsAsync(email, ct);
        if (invites.Count == 0)
            return [];

        var organizations = await organizationRepository.GetByIdsAsync(invites.Select(i => i.OrganizationId).Distinct(), ct);
        var organizationNames = organizations.ToDictionary(o => o.Id, o => o.Nome);

        return invites
            .Select(i => new PendingInviteDto(
                i.Id,
                i.OrganizationId,
                organizationNames.TryGetValue(i.OrganizationId, out var nome) ? nome : "Organização removida",
                i.Role,
                i.ExpiresAt))
            .OrderBy(d => d.ExpiresAt)
            .ToList();
    }
}
