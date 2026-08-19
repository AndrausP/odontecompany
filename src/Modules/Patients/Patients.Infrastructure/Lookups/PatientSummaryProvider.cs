using Microsoft.EntityFrameworkCore;
using Patients.Contracts;
using Patients.Infrastructure.Persistence;

namespace Patients.Infrastructure.Lookups;

/// <summary>Implementação de <see cref="IPatientSummaryProvider"/>. Filtra por OrganizationId explícito (não só o filtro global ambiente) — mesmo racional defensivo de PatientRepository.CpfExistsAsync.</summary>
public sealed class PatientSummaryProvider : IPatientSummaryProvider
{
    private readonly PatientsDbContext _context;

    public PatientSummaryProvider(PatientsDbContext context) => _context = context;

    public Task<int> ContarAtivosAsync(Guid organizationId, CancellationToken ct = default)
        => _context.Patients.AsNoTracking().IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de PatientRepository.CpfExistsAsync
            .CountAsync(p => p.OrganizationId == organizationId && p.Ativo, ct);
}
