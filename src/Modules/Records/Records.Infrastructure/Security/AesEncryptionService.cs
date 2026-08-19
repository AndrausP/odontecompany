using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Records.Application.Interfaces;

namespace Records.Infrastructure.Security;

/// <summary>
/// AES-256-GCM pra campos clínicos sensíveis em repouso (ex: <c>EvolucaoClinica.DescricaoClinica</c>).
/// A chave configurada (<see cref="RecordsEncryptionOptions.KeyBase64"/>) é hasheada via SHA-256
/// pra sempre virar exatamente 32 bytes, independente do tamanho/formato do valor configurado —
/// evita <see cref="ArgumentException"/> em runtime por chave com tamanho errado.
/// Formato do ciphertext persistido (base64 de): nonce(12) || tag(16) || dado cifrado.
/// Stateless — seguro como Singleton (registrado assim no DI porque é usado dentro de
/// <c>OnModelCreating</c> do <c>RecordsDbContext</c>, que roda uma vez por processo pro EF Core
/// compilar o Model, não por request).
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public AesEncryptionService(IOptions<RecordsEncryptionOptions> options)
    {
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.KeyBase64));
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, payload, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return ciphertext;

        var payload = Convert.FromBase64String(ciphertext);

        var nonce = payload.AsSpan(0, NonceSizeBytes);
        var tag = payload.AsSpan(NonceSizeBytes, TagSizeBytes);
        var cipherBytes = payload.AsSpan(NonceSizeBytes + TagSizeBytes);
        var plaintextBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
