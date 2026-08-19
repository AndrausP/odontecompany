using MediatR;
using SharedKernel;
using Subscriptions.Contracts;
using Subscriptions.Domain.Enums;

namespace Subscriptions.Application.Commands.SelectPlan;

/// <summary>OrganizationId nunca vem do corpo — sempre do organization_id do JWT do usuário autenticado.</summary>
public sealed record SelectPlanCommand(Guid OrganizationId, PlanTier Tier) : IRequest<Result<SubscriptionDto>>;
