using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subscriptions.Application.Commands.SelectPlan;
using Subscriptions.Application.Commands.StartCheckout;
using Subscriptions.Application.Queries.GetCurrentSubscription;
using Subscriptions.Contracts;
using Subscriptions.Domain;
using Subscriptions.Domain.Enums;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Assinatura da PLATAFORMA (organization paga o OdontoPlatform) — não confundir com
/// `/api/faturas` (Billing, paciente paga a clínica). Checkout de verdade (Stripe) é esqueleto
/// nesta rodada (sprint-11): <see cref="Checkout"/> não cobra ninguém, só exercita o contrato.
/// </summary>
[ApiController]
[Route("api/subscriptions")]
[Authorize]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public SubscriptionsController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Catálogo estático dos 3 planos — mesmo conteúdo exibido na landing (frontend/src/features/marketing/LandingPage.tsx), fonte única em Subscriptions.Domain.PlanCatalog.</summary>
    [HttpGet("plans")]
    public IActionResult GetPlans()
    {
        var plans = PlanCatalog.All()
            .Select(p => new PlanDto(p.Tier.ToString(), p.Nome, p.LimiteFiliais, p.PrecoMensal))
            .ToList();

        return Ok(plans);
    }

    /// <summary>Assinatura ativa da organização do token — null se ainda não escolheu plano nenhum (onboarding incompleto).</summary>
    [HttpGet("me")]
    [Authorize(Policy = "RequireActiveOrganization")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new GetCurrentSubscriptionQuery(organizationId.Value), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Escolhe (ou troca) o plano da organização do token. Sem cobrança nesta rodada — vira ativo direto.</summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Admin", Policy = "RequireActiveOrganization")]
    public async Task<IActionResult> SelectPlan([FromBody] SelectPlanRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new SelectPlanCommand(organizationId.Value, request.Tier), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Inicia checkout no gateway de pagamento — ESQUELETO (sprint-11): não chama Stripe de
    /// verdade, devolve URL de placeholder. Ver Subscriptions.Infrastructure.Payments.StripePaymentGatewayService.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize(Roles = "Owner,Admin", Policy = "RequireActiveOrganization")]
    public async Task<IActionResult> Checkout([FromBody] SelectPlanRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new StartCheckoutCommand(organizationId.Value, request.Tier), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }
}

public sealed record SelectPlanRequest(PlanTier Tier);
