using MediatR;
using SharedKernel;
using Subscriptions.Contracts;

namespace Subscriptions.Application.Queries.GetCurrentSubscription;

/// <summary>Value (não Result.Failure) quando não existe assinatura — "sem plano ainda" é estado normal, não erro.</summary>
public sealed record GetCurrentSubscriptionQuery(Guid OrganizationId) : IRequest<Result<SubscriptionDto?>>;
