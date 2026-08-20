using Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Notifications;

/// <summary>
/// Implementação de referência de <see cref="IPasswordResetNotifier"/> — NÃO envia email nenhum,
/// só loga o token gerado. Mesmo padrão e mesma dívida nomeada de <see cref="LoggingInviteNotifier"/>
/// (provider real de email é task futura, troca as duas implementações juntas quando existir).
/// </summary>
public sealed class LoggingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ILogger<LoggingPasswordResetNotifier> _logger;

    public LoggingPasswordResetNotifier(ILogger<LoggingPasswordResetNotifier> logger) => _logger = logger;

    public Task SendResetLinkAsync(string email, string plainToken, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Redefinição de senha pedida para {Email} — token (sem provider de email real, ver Identity.Infrastructure.Notifications.LoggingPasswordResetNotifier): {Token}",
            email, plainToken);

        return Task.CompletedTask;
    }
}
