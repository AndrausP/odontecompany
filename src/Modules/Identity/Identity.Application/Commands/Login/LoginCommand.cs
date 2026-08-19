using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResultDto>>;
