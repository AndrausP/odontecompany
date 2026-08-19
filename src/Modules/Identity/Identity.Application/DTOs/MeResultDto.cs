namespace Identity.Application.DTOs;

/// <summary>
/// Snapshot do estado do usuário autenticado (task 017) — tudo que o frontend precisa pra montar
/// cabeçalho/seletor de organization numa única ida: dados do usuário, organizations a que
/// pertence (com papel em cada), qual está ativa no token atual (<c>null</c> se o token não tiver
/// organization) e convites pendentes (embutido, além do endpoint separado <c>GET /api/me/invites</c>).
/// </summary>
/// <summary>
/// `ActivePlanTier` (sprint-11) — plano ativo da `ActiveOrganizationId`, null se a organização
/// ainda não escolheu nenhum (onboarding incompleto) ou se não há organization ativa. Resolvido
/// via `Subscriptions.Contracts.ISubscriptionLookup` — Identity nunca depende de
/// Subscriptions.Domain/Infrastructure.
/// </summary>
public sealed record MeResultDto(UserDto User, List<OrganizationMembershipDto> Organizations, Guid? ActiveOrganizationId, string? ActivePlanTier, List<PendingInviteDto> PendingInvites);
