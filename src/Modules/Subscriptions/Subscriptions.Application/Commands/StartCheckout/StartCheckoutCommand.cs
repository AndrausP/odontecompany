using MediatR;
using SharedKernel;
using Subscriptions.Application.Interfaces;
using Subscriptions.Domain.Enums;

namespace Subscriptions.Application.Commands.StartCheckout;

/// <summary>
/// Endpoint de checkout de verdade (esqueleto, sprint-11) — separado de `SelectPlanCommand` de
/// propósito: escolher plano hoje não cobra nada (upsert direto, `Status=Ativa`); iniciar
/// checkout é o gancho pronto pro dia em que o Stripe estiver conectado (troca só a
/// implementação de `IPaymentGatewayService`, este Command/Handler não muda).
/// </summary>
public sealed record StartCheckoutCommand(Guid OrganizationId, PlanTier Tier) : IRequest<Result<CheckoutSessionResult>>;
