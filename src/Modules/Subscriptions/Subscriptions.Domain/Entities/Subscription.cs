using SharedKernel;
using Subscriptions.Domain.Enums;
using Subscriptions.Domain.Errors;

namespace Subscriptions.Domain.Entities;

/// <summary>
/// Assinatura da PLATAFORMA (Organization paga o OdontoPlatform) — não confundir com
/// `Billing.Fatura` (paciente paga a clínica), domínio irmão mas separado (sprint-11, ver
/// docs/decisions.md). Uma por Organization (índice único), 1:1. `StripeCustomerId`/
/// `StripeSubscriptionId` ficam null até o checkout real existir — hoje toda assinatura nasce
/// <see cref="SubscriptionStatus.Ativa"/> sem cobrança nenhuma (esqueleto).
/// </summary>
public class Subscription : AggregateRoot, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public PlanTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }

    private Subscription() { } // EF Core

    private Subscription(Guid organizationId, PlanTier tier)
    {
        OrganizationId = organizationId;
        Tier = tier;
        Status = SubscriptionStatus.Ativa;
    }

    public static Result<Subscription> Create(Guid organizationId, PlanTier tier)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Subscription>(DomainErrors.Subscription.OrganizationInvalido);

        return Result.Success(new Subscription(organizationId, tier));
    }

    /// <summary>Troca de plano — Owner pode fazer upgrade/downgrade a qualquer momento (sem lógica de proration nesta rodada, esqueleto).</summary>
    public void TrocarPlano(PlanTier novoTier)
    {
        Tier = novoTier;
        SetUpdatedAt();
    }

    public void Cancelar()
    {
        Status = SubscriptionStatus.Cancelada;
        SetUpdatedAt();
    }

    /// <summary>Chamado quando o checkout do Stripe existir de verdade — associa os ids externos assim que o customer/subscription forem criados do lado do Stripe.</summary>
    public void VincularStripe(string stripeCustomerId, string stripeSubscriptionId)
    {
        StripeCustomerId = stripeCustomerId;
        StripeSubscriptionId = stripeSubscriptionId;
        SetUpdatedAt();
    }
}
