using Subscriptions.Domain.Enums;

namespace Subscriptions.Application.Interfaces;

/// <summary>Resultado de uma tentativa de checkout — URL pra onde o front redireciona o usuário completar o pagamento no provedor.</summary>
public sealed record CheckoutSessionResult(string CheckoutUrl, string? ProviderSessionId);

/// <summary>
/// Porta (ACL) pro provedor de pagamento externo — mesmo padrão de
/// <c>Billing.Application.Interfaces.IConvenioAdapter</c> (ver docs/decisions.md): a Application
/// nunca fala com SDK de Stripe direto, só com esta interface. Implementação de referência
/// (<c>Subscriptions.Infrastructure.Payments.StripePaymentGatewayService</c>) é esqueleto —
/// sprint-11 pediu "sistema de planos com esqueleto de Stripe, conexão ainda não precisa
/// funcionar" — troca por chamada real ao SDK do Stripe (pacote `Stripe.net`) quando a conta/API
/// key existir, sem tocar em nenhum CommandHandler que já usa esta porta.
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>Inicia um checkout pro plano escolhido. Esqueleto: não chama nenhuma API externa, devolve uma URL de placeholder.</summary>
    Task<CheckoutSessionResult> CriarCheckoutSessionAsync(Guid organizationId, PlanTier tier, CancellationToken ct = default);
}
