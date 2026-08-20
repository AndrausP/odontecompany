using Scheduling.Domain.Entities;

namespace Scheduling.Application.Interfaces;

public interface IProcedimentoRepository
{
    Task<Procedimento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Procedimento procedimento, CancellationToken ct = default);

    /// <summary>Listagem simples (sem paginação — volume esperado é baixo, dezenas por clínica) escopada pelo filtro global de organization.</summary>
    Task<IReadOnlyList<Procedimento>> ListAsync(bool includeInactive, CancellationToken ct = default);
}
