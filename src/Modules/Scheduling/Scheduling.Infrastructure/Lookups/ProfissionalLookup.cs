using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Lookups;

/// <summary>Implementação de <see cref="IProfissionalLookup"/> — mesmo padrão de <c>Tenancy.Infrastructure.Lookups.BranchLookup</c>.</summary>
public sealed class ProfissionalLookup : IProfissionalLookup
{
    private readonly SchedulingDbContext _context;

    public ProfissionalLookup(SchedulingDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProfissionalResumoDto>> ListarPorOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await _context.Profissionais
            .AsNoTracking()
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de BranchLookup
            .Where(p => p.OrganizationId == organizationId) // sem filtro de Ativo — inclui inativos de propósito
            .Select(p => new ProfissionalResumoDto(p.Id, p.Nome, p.UserId, p.BranchId, p.Ativo))
            .ToListAsync(ct);
}
