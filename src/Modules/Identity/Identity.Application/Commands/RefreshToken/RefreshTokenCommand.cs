using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

// Namespace "Refresh" (não "RefreshToken") de propósito: evita colisão de símbolo entre o
// nome do namespace e o tipo Identity.Domain.Entities.RefreshToken usado no handler.
namespace Identity.Application.Commands.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<LoginResultDto>>;
