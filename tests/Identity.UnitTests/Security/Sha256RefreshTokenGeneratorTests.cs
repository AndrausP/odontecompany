using Identity.Infrastructure.Security;

namespace Identity.UnitTests.Security;

[TestFixture]
public class Sha256RefreshTokenGeneratorTests
{
    private Sha256RefreshTokenGenerator _generator;

    [SetUp]
    public void Setup() => _generator = new Sha256RefreshTokenGenerator();

    [Test]
    public void Should_ProduceMatchingHash_When_HashingTheGeneratedPlainToken()
    {
        var (plainToken, tokenHash) = _generator.Generate();

        Assert.That(_generator.Hash(plainToken), Is.EqualTo(tokenHash));
    }

    [Test]
    public void Should_ProduceDifferentTokens_When_CalledTwice()
    {
        var (plain1, _) = _generator.Generate();
        var (plain2, _) = _generator.Generate();

        Assert.That(plain1, Is.Not.EqualTo(plain2));
    }

    [Test]
    public void Should_NeverExposePlainTokenAsHash()
    {
        var (plainToken, tokenHash) = _generator.Generate();

        Assert.That(tokenHash, Is.Not.EqualTo(plainToken), "hash persistido nunca pode ser igual ao valor em claro");
    }
}
