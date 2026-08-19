using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.AcceptInvite;

/// <summary>UserId nunca vem do corpo — sempre do sub do JWT do usuário autenticado. Token é o valor em claro recebido na URL.</summary>
public sealed record AcceptInviteCommand(Guid UserId, string Token) : IRequest<Result<AcceptInviteResultDto>>;
