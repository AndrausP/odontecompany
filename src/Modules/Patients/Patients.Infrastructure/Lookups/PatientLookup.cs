using Microsoft.EntityFrameworkCore;
using Patients.Contracts;
using Patients.Infrastructure.Persistence;

namespace Patients.Infrastructure.Lookups;

/// <summary>Implementação de <see cref="IPatientLookup"/> — porta pública pra outros módulos (ex: Scheduling) checarem paciente.</summary>
public sealed class PatientLookup : IPatientLookup
{
    private readonly PatientsDbContext _context;

    public PatientLookup(PatientsDbContext context) => _context = context;

    public Task<bool> ExistsAsync(Guid patientId, CancellationToken ct = default)
        => _context.Patients.AsNoTracking().AnyAsync(p => p.Id == patientId && p.Ativo, ct);
}
