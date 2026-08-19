namespace Identity.Application.Interfaces;

/// <summary>
/// Porta pra geração/hash de token de convite (task 016) — mesmo padrão de
/// <see cref="IRefreshTokenGenerator"/> (segredo opaco de alta entropia gerado pelo servidor,
/// SHA-256 é suficiente, não é senha escolhida por humano). Classe própria (não reaproveita
/// <see cref="IRefreshTokenGenerator"/>) de propósito: são conceitos de domínio diferentes
/// (convite vs sessão), mesmo a implementação de baixo nível sendo idêntica hoje.
/// </summary>
public interface IInviteTokenGenerator
{
    /// <summary>Gera um novo token de convite: o valor em claro (devolvido na resposta / logado pelo IInviteNotifier) e o hash (persistido).</summary>
    (string PlainToken, string TokenHash) Generate();

    /// <summary>Calcula o hash de um token recebido do cliente, pra lookup na base.</summary>
    string Hash(string plainToken);
}
