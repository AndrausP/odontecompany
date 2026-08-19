using Identity.Domain.Entities;

namespace Identity.UnitTests.Domain;

[TestFixture]
public class RefreshTokenTests
{
    [Test]
    public void Should_BeActive_When_JustCreated()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        Assert.That(token.IsActive, Is.True);
        Assert.That(token.IsExpired, Is.False);
        Assert.That(token.IsRevoked, Is.False);
    }

    [Test]
    public void Should_BeExpired_When_LifetimeIsInThePast()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", TimeSpan.FromSeconds(-1));

        Assert.That(token.IsExpired, Is.True);
        Assert.That(token.IsActive, Is.False);
    }

    [Test]
    public void Should_BeRevoked_When_Revoke_IsCalled()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.Revoke("hash-substituto");

        Assert.That(token.IsRevoked, Is.True);
        Assert.That(token.IsActive, Is.False);
        Assert.That(token.ReplacedByTokenHash, Is.EqualTo("hash-substituto"));
    }
}
