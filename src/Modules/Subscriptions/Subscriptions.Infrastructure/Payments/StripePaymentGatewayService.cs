using Microsoft.Extensions.Logging;
using Subscriptions.Application.Interfaces;
using Subscriptions.Domain;
using Subscriptions.Domain.Enums;

namespace Subscriptions.Infrastructure.Payments;

/// <summary>
/// Implementação de referência de <see cref="IPaymentGatewayService"/> — ESQUELETO (sprint-11,
/// pedido explícito do usuário: "conexão com Stripe ainda não precisa funcionar mas deve ter o
/// esqueleto já"). Não adiciona o pacote `Stripe.net` nem chama nenhuma API externa — só loga a
/// intenção e devolve uma URL de placeholder, o suficiente pra exercitar
/// `StartCheckoutCommand`/`POST /api/subscriptions/checkout` ponta a ponta sem cobrar ninguém.
///
/// Pra virar real: adicionar `Stripe.net` neste projeto, ler `STRIPE_SECRET_KEY` de
/// configuração (nunca hardcoded), trocar o corpo deste método por
/// `SessionService.CreateAsync(...)` do SDK, devolvendo `session.Url`/`session.Id` de verdade.
/// Nenhum CommandHandler muda — só esta classe, porque tudo passa pela porta
/// <see cref="IPaymentGatewayService"/> (mesmo padrão de `IConvenioAdapter`/
/// `ManualConvenioAdapter` do módulo Billing, ver docs/decisions.md).
/// </summary>
public sealed class StripePaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<StripePaymentGatewayService> _logger;

    public StripePaymentGatewayService(ILogger<StripePaymentGatewayService> logger)
    {
        _logger = logger;
    }

    public Task<CheckoutSessionResult> CriarCheckoutSessionAsync(Guid organizationId, PlanTier tier, CancellationToken ct = default)
    {
        var plano = PlanCatalog.Get(tier);

        _logger.LogInformation(
            "[Stripe esqueleto] Checkout NÃO real solicitado — organization {OrganizationId}, plano {Plano} (R$ {Preco}/mês). " +
            "Stripe.net não está integrado ainda; nenhuma chamada externa foi feita.",
            organizationId, plano.Nome, plano.PrecoMensal);

        var fakeSessionId = $"cs_skeleton_{Guid.NewGuid():N}";
        var placeholderUrl = $"about:blank#stripe-checkout-esqueleto-{fakeSessionId}";

        return Task.FromResult(new CheckoutSessionResult(placeholderUrl, fakeSessionId));
    }
}
