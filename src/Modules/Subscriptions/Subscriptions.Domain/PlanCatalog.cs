using Subscriptions.Domain.Enums;

namespace Subscriptions.Domain;

/// <summary>Metadata de 1 plano do catálogo — espelha os 3 cards da landing (frontend/src/features/marketing/LandingPage.tsx).</summary>
public sealed record PlanInfo(PlanTier Tier, string Nome, int LimiteFiliais, decimal PrecoMensal, string? StripePriceId);

/// <summary>
/// Fonte única do que cada plano libera — landing (preço exibido), onboarding (seletor) e
/// enforcement de limite de filial (<see cref="Tenancy"/>, via <c>ISubscriptionLookup</c>) leem
/// daqui, nunca duplicam o número em outro lugar. `StripePriceId` é null pra todos: esqueleto
/// (sprint-11) — preenchido quando os produtos forem criados de verdade no dashboard do Stripe.
/// </summary>
public static class PlanCatalog
{
    private static readonly IReadOnlyDictionary<PlanTier, PlanInfo> Planos = new Dictionary<PlanTier, PlanInfo>
    {
        [PlanTier.Starter] = new PlanInfo(PlanTier.Starter, "Starter", LimiteFiliais: 1, PrecoMensal: 129m, StripePriceId: null),
        [PlanTier.Profissional] = new PlanInfo(PlanTier.Profissional, "Profissional", LimiteFiliais: 3, PrecoMensal: 349m, StripePriceId: null),
        // int.MaxValue representa "ilimitado" — evita Nullable<int> se espalhando em toda
        // comparação de limite (CreateBranchCommandHandler faz só `count >= LimiteFiliais`).
        [PlanTier.Rede] = new PlanInfo(PlanTier.Rede, "Rede", LimiteFiliais: int.MaxValue, PrecoMensal: 0m, StripePriceId: null),
    };

    public static PlanInfo Get(PlanTier tier) => Planos[tier];

    public static IReadOnlyCollection<PlanInfo> All() => Planos.Values.ToList();
}
