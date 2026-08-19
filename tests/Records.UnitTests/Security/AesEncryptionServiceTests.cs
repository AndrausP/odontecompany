using Microsoft.Extensions.Options;
using Records.Infrastructure.Security;

namespace Records.UnitTests.Security;

[TestFixture]
public class AesEncryptionServiceTests
{
    private static AesEncryptionService CreateService(string key = "test-key-32-bytes-minimum-material")
        => new(Options.Create(new RecordsEncryptionOptions { KeyBase64 = key }));

    [Test]
    public void Should_ReturnOriginalPlaintext_When_DecryptingWhatWasEncrypted()
    {
        var service = CreateService();
        const string plaintext = "Paciente relata dor no dente 26, indicação de canal.";

        var ciphertext = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(ciphertext);

        Assert.That(decrypted, Is.EqualTo(plaintext));
    }

    [Test]
    public void Should_ProduceCiphertextDifferentFromPlaintext_When_Encrypting()
    {
        var service = CreateService();
        const string plaintext = "Dado clínico sensível.";

        var ciphertext = service.Encrypt(plaintext);

        Assert.That(ciphertext, Is.Not.EqualTo(plaintext));
        Assert.That(ciphertext, Does.Not.Contain(plaintext));
    }

    [Test]
    public void Should_ProduceDifferentCiphertexts_When_EncryptingSamePlaintextTwice()
    {
        // Nonce aleatório por chamada — mesmo plaintext nunca produz o mesmo ciphertext duas
        // vezes, mesmo com a mesma chave (propriedade de segurança do AES-GCM: previne análise
        // de padrão em dados repetidos, ex: duas evoluções clínicas com texto igual).
        var service = CreateService();
        const string plaintext = "Consulta de rotina.";

        var ciphertext1 = service.Encrypt(plaintext);
        var ciphertext2 = service.Encrypt(plaintext);

        Assert.That(ciphertext1, Is.Not.EqualTo(ciphertext2));
        Assert.That(service.Decrypt(ciphertext1), Is.EqualTo(plaintext));
        Assert.That(service.Decrypt(ciphertext2), Is.EqualTo(plaintext));
    }

    [Test]
    public void Should_ThrowOrFailDecryption_When_UsingADifferentKey()
    {
        var serviceA = CreateService("chave-do-organization-a");
        var serviceB = CreateService("chave-completamente-diferente-b");

        var ciphertext = serviceA.Encrypt("dado sensível");

        // AES-GCM autentica o ciphertext (tag) — chave errada não decripta "lixo", lança
        // exception (AuthenticationTagMismatchException/CryptographicException).
        Assert.Throws<System.Security.Cryptography.AuthenticationTagMismatchException>(
            () => serviceB.Decrypt(ciphertext));
    }

    [Test]
    public void Should_ReturnEmptyString_When_EncryptingEmptyString()
    {
        var service = CreateService();

        Assert.That(service.Encrypt(string.Empty), Is.EqualTo(string.Empty));
        Assert.That(service.Decrypt(string.Empty), Is.EqualTo(string.Empty));
    }
}
