using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NovaSenha) : IRequest<Result>;
