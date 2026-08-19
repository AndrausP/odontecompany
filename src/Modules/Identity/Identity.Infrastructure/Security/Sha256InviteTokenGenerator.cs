using System.Security.Cryptography;
using System.Text;
using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Token de convite é um segredo opaco de 256 bits de entropia gerado pelo servidor — mesmo
/// raciocínio de <see cref="Sha256RefreshTokenGenerator"/> (SHA-256 é suficiente, Argon2id fica
/// reservado pra senha de usuário). Classe própria (não a mesma de RefreshToken) por serem
/// conceitos de domínio diferentes, mesmo com implementação de baixo nível idêntica hoje.
/// </summary>
public sealed class Sha256InviteTokenGenerator : IInviteTokenGenerator
{
    public (string PlainToken, string TokenHash) Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var plainToken = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        return (plainToken, Hash(plainToken));
    }

    public string Hash(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes);
    }
}
