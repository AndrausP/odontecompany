using Scheduling.Domain.Entities;

namespace Scheduling.Application.Interfaces;

public interface ISalaRepository
{
    Task<Sala?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Sala sala, CancellationToken ct = default);

    /// <summary>Listagem simples (sem paginação — volume esperado é baixo) escopada pelo filtro global de organization.</summary>
    Task<IReadOnlyList<Sala>> ListAsync(bool includeInactive, CancellationToken ct = default);
}
