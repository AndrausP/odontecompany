using Tenancy.Domain.Entities;

namespace Tenancy.Application.Interfaces;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Branch branch, CancellationToken ct = default);
    Task<IReadOnlyList<Branch>> ListByOrganizationAsync(Guid organizationId, bool includeInactive, CancellationToken ct = default);
}
