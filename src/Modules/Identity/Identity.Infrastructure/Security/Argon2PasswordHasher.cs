using System.Security.Cryptography;
using System.Text;
using Identity.Application.Interfaces;
using Konscious.Security.Cryptography;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Hash de senha via Argon2id (recomendação OWASP atual). Parâmetros: 64 MB de memória,
/// 3 iterações, paralelismo 4 — equilíbrio custo/segurança razoável pra API (ajustar se o
/// hardware de produção permitir mais).
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "argon2id";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 3;
    private const int MemorySizeKb = 65536; // 64 MB
    private const int Parallelism = 4;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt, Iterations, MemorySizeKb, Parallelism, HashSize);

        return $"{Prefix}${Iterations}${MemorySizeKb}${Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 6 || parts[0] != Prefix)
            return false;

        if (!int.TryParse(parts[1], out var iterations) ||
            !int.TryParse(parts[2], out var memoryKb) ||
            !int.TryParse(parts[3], out var parallelism))
            return false;

        byte[] salt, expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[4]);
            expectedHash = Convert.FromBase64String(parts[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        var computedHash = ComputeHash(password, salt, iterations, memoryKb, parallelism, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt, int iterations, int memoryKb, int parallelism, int hashSize)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            Iterations = iterations,
            MemorySize = memoryKb
        };

        return argon2.GetBytes(hashSize);
    }
}
