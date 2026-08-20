using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Pedido de redefinição de senha. Sempre devolve sucesso, exista ou não o email — anti-
/// enumeração (mesmo raciocínio do login/signup: nunca confirmar se um email está cadastrado).
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
