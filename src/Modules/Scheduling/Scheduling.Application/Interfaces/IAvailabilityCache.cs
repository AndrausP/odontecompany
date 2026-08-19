using Scheduling.Contracts;

namespace Scheduling.Application.Interfaces;

/// <summary>Cache-aside de disponibilidade (slots livres) por profissional/dia. Ver <c>SchedulingCacheKeys</c> pra convenção de chave.</summary>
public interface IAvailabilityCache
{
    Task<IReadOnlyList<AvailabilitySlotDto>?> GetAsync(string key, CancellationToken ct = default);

    Task SetAsync(string key, IReadOnlyList<AvailabilitySlotDto> slots, TimeSpan ttl, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}
