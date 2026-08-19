using Identity.Infrastructure.Security;

namespace Identity.UnitTests.Security;

[TestFixture]
public class Argon2PasswordHasherTests
{
    private Argon2PasswordHasher _hasher;

    [SetUp]
    public void Setup() => _hasher = new Argon2PasswordHasher();

    [Test]
    public void Should_VerifyPassword_When_HashMatches()
    {
        var hash = _hasher.Hash("Senha@123");

        Assert.That(_hasher.Verify("Senha@123", hash), Is.True);
    }

    [Test]
    public void Should_RejectPassword_When_PasswordIsWrong()
    {
        var hash = _hasher.Hash("Senha@123");

        Assert.That(_hasher.Verify("Senha-Errada", hash), Is.False);
    }

    [Test]
    public void Should_ProduceDifferentHashes_When_HashingSamePasswordTwice()
    {
        var hash1 = _hasher.Hash("Senha@123");
        var hash2 = _hasher.Hash("Senha@123");

        Assert.That(hash1, Is.Not.EqualTo(hash2), "salt aleatório precisa gerar hashes diferentes pra mesma senha");
        Assert.That(_hasher.Verify("Senha@123", hash1), Is.True);
        Assert.That(_hasher.Verify("Senha@123", hash2), Is.True);
    }

    [Test]
    public void Should_RejectPassword_When_HashIsMalformed()
    {
        Assert.That(_hasher.Verify("qualquer", "isso-nao-e-um-hash-valido"), Is.False);
    }
}
