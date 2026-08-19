using Billing.Domain.Entities;

namespace Billing.Application.Interfaces;

public interface IConvenioRepository
{
    Task<Convenio?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Convenio convenio, CancellationToken ct = default);

    /// <summary>Listagem simples (sem paginação — volume esperado é baixo) escopada pelo filtro global de organization.</summary>
    Task<IReadOnlyList<Convenio>> ListAsync(bool includeInactive, CancellationToken ct = default);
}
