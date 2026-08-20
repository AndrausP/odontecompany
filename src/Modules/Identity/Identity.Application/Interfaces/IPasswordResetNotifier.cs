namespace Identity.Application.Interfaces;

/// <summary>
/// Porta de envio do link de redefinição pro usuário. Mesmo padrão de <see cref="IInviteNotifier"/>
/// — implementação desta rodada é NO-OP (só loga, ver <c>LoggingPasswordResetNotifier</c>,
/// Infrastructure). Provider real de email é a MESMA dívida técnica já nomeada em
/// <see cref="IInviteNotifier"/>, não uma nova — trocar os dois juntos quando o provider existir.
/// </summary>
public interface IPasswordResetNotifier
{
    Task SendResetLinkAsync(string email, string plainToken, CancellationToken ct = default);
}
