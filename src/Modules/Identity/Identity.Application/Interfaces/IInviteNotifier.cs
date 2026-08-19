namespace Identity.Application.Interfaces;

/// <summary>
/// Porta de envio do convite pro convidado (task 016). Implementação desta sprint é NO-OP —
/// só loga o token (ver <c>LoggingInviteNotifier</c>, Infrastructure). Provider real de email é
/// task futura, fora deste sprint (débito nomeado, bloqueante antes de qualquer usuário real).
/// </summary>
public interface IInviteNotifier
{
    Task SendInviteAsync(string email, string plainToken, Guid organizationId, CancellationToken ct = default);
}
