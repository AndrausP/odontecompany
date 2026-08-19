using System.Text.Json;
using Scheduling.Application.Interfaces;
using Scheduling.Contracts;
using StackExchange.Redis;

namespace Scheduling.Infrastructure.Caching;

/// <summary>Cache-aside de disponibilidade via <c>IDatabase.StringGetAsync</c>/<c>StringSetAsync</c>, payload JSON.</summary>
public sealed class RedisAvailabilityCache : IAvailabilityCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisAvailabilityCache(IConnectionMultiplexer connectionMultiplexer) => _connectionMultiplexer = connectionMultiplexer;

    public async Task<IReadOnlyList<AvailabilitySlotDto>?> GetAsync(string key, CancellationToken ct = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var cached = await database.StringGetAsync(key);

        if (cached.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<List<AvailabilitySlotDto>>(cached!, SerializerOptions);
    }

    public async Task SetAsync(string key, IReadOnlyList<AvailabilitySlotDto> slots, TimeSpan ttl, CancellationToken ct = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var json = JsonSerializer.Serialize(slots, SerializerOptions);
        await database.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        await database.KeyDeleteAsync(key);
    }
}
