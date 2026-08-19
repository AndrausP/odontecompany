namespace Subscriptions.Domain.Enums;

/// <summary>
/// Sem "Trialing" nesta rodada — Stripe ainda não cobra de verdade (esqueleto, sprint-11), então
/// toda assinatura nasce direto <see cref="Ativa"/>. Trial/PagamentoPendente entram quando o
/// checkout do Stripe existir de fato (ver <see cref="Subscriptions.Application.Interfaces.IPaymentGatewayService"/>).
/// </summary>
public enum SubscriptionStatus
{
    Ativa = 1,
    Cancelada = 2,
}
