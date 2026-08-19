using System.Security.Cryptography;
using System.Text;
using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Refresh token é um segredo opaco de 256 bits de entropia gerado pelo servidor — não é
/// senha escolhida por humano, então SHA-256 é suficiente pra proteção em repouso (Argon2id
/// fica reservado pra senha de usuário, que é vulnerável a dicionário/força bruta).
/// </summary>
public sealed class Sha256RefreshTokenGenerator : IRefreshTokenGenerator
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
