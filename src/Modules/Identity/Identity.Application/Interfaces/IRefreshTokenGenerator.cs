namespace Identity.Application.Interfaces;

/// <summary>
/// Porta pra geração/hash de refresh token. Token é um segredo opaco de alta entropia — não
/// é senha escolhida por humano, então não precisa de Argon2id (isso é reservado pra senha
/// de usuário); a implementação de Infrastructure usa hash simples (SHA-256).
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Gera um novo refresh token: o valor em claro (devolvido ao cliente) e o hash (persistido).</summary>
    (string PlainToken, string TokenHash) Generate();

    /// <summary>Calcula o hash de um token recebido do cliente, pra lookup na base.</summary>
    string Hash(string plainToken);
}
