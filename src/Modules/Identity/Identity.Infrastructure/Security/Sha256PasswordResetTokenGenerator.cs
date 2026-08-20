using System.Security.Cryptography;
using System.Text;
using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Token de redefinição de senha é um segredo opaco de 256 bits de entropia gerado pelo servidor
/// — mesmo raciocínio de <see cref="Sha256InviteTokenGenerator"/>/<see cref="Sha256RefreshTokenGenerator"/>.
/// </summary>
public sealed class Sha256PasswordResetTokenGenerator : IPasswordResetTokenGenerator
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
