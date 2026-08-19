using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.Signup;

/// <summary>
/// Signup público (task 015) — revoga a regra antiga "sem registro público, só seed + admin
/// autenticado". Cria um <see cref="Identity.Domain.Entities.User"/> GLOBAL, sem organization e
/// sem role: criar (ou aderir a) uma organization é sempre um passo SEPARADO em seguida
/// (<c>POST /api/organizations</c> — task 015 — ou <c>POST /api/invites/{token}/accept</c> — task 016).
/// </summary>
public sealed record SignupCommand(string Nome, string Email, string Password) : IRequest<Result<SignupResultDto>>;
