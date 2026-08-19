namespace Subscriptions.Contracts;

/// <summary>Espelha Subscriptions.Domain.PlanInfo — DTO público (enum como string, não int cru, pro frontend).</summary>
public sealed record PlanDto(string Tier, string Nome, int LimiteFiliais, decimal PrecoMensal);

/// <summary>Espelha Subscriptions.Domain.Entities.Subscription.</summary>
public sealed record SubscriptionDto(Guid Id, Guid OrganizationId, string Tier, string Status, DateTime CriadoEm);
