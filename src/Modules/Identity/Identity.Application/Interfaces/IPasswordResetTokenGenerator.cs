namespace Identity.Application.Interfaces;

/// <summary>
/// Porta pra geração/hash de token de redefinição de senha — mesmo padrão de
/// <see cref="IInviteTokenGenerator"/>/<see cref="IRefreshTokenGenerator"/> (segredo opaco de alta
/// entropia gerado pelo servidor, SHA-256 é suficiente). Classe própria de propósito, mesmo
/// raciocínio dos outros dois: conceito de domínio diferente, implementação de baixo nível igual.
/// </summary>
public interface IPasswordResetTokenGenerator
{
    (string PlainToken, string TokenHash) Generate();

    string Hash(string plainToken);
}
