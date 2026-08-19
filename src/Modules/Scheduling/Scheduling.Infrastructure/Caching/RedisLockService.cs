using Scheduling.Application.Interfaces;
using StackExchange.Redis;

namespace Scheduling.Infrastructure.Caching;

/// <summary>
/// Lock distribuído curto via <c>IDatabase.LockTakeAsync</c>/<c>LockReleaseAsync</c> (SET NX +
/// comparação de token na liberação, nativo do StackExchange.Redis). Conexão é resolvida em
/// runtime (via <see cref="IConnectionMultiplexer"/> singleton registrado com
/// AbortOnConnectFail=false) — se o Redis estiver fora do ar, os métodos aqui lançam/retornam
/// falha de forma isolada, nunca derrubam o processo no startup.
/// </summary>
public sealed class RedisLockService : IRedisLockService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisLockService(IConnectionMultiplexer connectionMultiplexer) => _connectionMultiplexer = connectionMultiplexer;

    public async Task<string?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var lockToken = Guid.NewGuid().ToString("N");

        var acquired = await database.LockTakeAsync(key, lockToken, ttl);

        return acquired ? lockToken : null;
    }

    public async Task ReleaseAsync(string key, string lockToken, CancellationToken ct = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        await database.LockReleaseAsync(key, lockToken);
    }
}
