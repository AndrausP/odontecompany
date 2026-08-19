namespace Subscriptions.Contracts;

/// <summary>
/// Porta pública do módulo Subscriptions pra outros módulos (Identity — gate de app sem plano;
/// Tenancy — limite de filial por plano) sem depender de Subscriptions.Domain/Infrastructure —
/// mesmo padrão de <c>Tenancy.Contracts.IBranchLookup</c>.
/// </summary>
public interface ISubscriptionLookup
{
    /// <summary>Assinatura ativa da organização, ou null se nunca escolheu um plano (ainda não passou pelo onboarding) ou cancelou.</summary>
    Task<SubscriptionDto?> ObterAtivaAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Quantas filiais (Branch) o plano ativo da organização permite. 0 se não tem plano ativo — quem chama decide o que fazer (bloquear criação).</summary>
    Task<int> LimiteDeFiliaisAsync(Guid organizationId, CancellationToken ct = default);
}
