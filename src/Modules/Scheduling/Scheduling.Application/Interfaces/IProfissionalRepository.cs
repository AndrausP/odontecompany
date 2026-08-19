using Scheduling.Domain.Entities;

namespace Scheduling.Application.Interfaces;

public interface IProfissionalRepository
{
    Task<Profissional?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Profissional profissional, CancellationToken ct = default);

    /// <summary>Listagem simples (sem paginação — volume esperado é baixo, dezenas por clínica) escopada pelo filtro global de organization.</summary>
    Task<IReadOnlyList<Profissional>> ListAsync(bool includeInactive, CancellationToken ct = default);
}
