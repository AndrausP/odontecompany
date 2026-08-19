using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

/// <summary>
/// Resultado da criação de convite (task 016). <see cref="Token"/> é o valor EM CLARO — só existe
/// neste momento (o banco guarda só o hash). Devolvido na resposta porque não há provider de
/// email real nesta sprint (<c>IInviteNotifier</c> no-op só loga) — sem isso o convite seria
/// inutilizável. Débito nomeado: expor o token na resposta HTTP é aceitável só enquanto não há
/// canal de entrega real (dev/test); bloqueante antes de produção (ver docs/decisions.md).
/// </summary>
public sealed record CreateInviteResultDto(Guid InviteId, string Email, Role Role, DateTime ExpiresAt, string Token);
