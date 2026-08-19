using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.SkipOnboarding;

/// <summary>UserId nunca vem do corpo — sempre do sub do JWT do usuário autenticado (mesmo padrão de AcceptInviteCommand).</summary>
public sealed record SkipOnboardingCommand(Guid UserId) : IRequest<Result>;
