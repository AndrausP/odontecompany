using Microsoft.EntityFrameworkCore;
using Patients.Application.Interfaces;
using Patients.Domain.Entities;
using Patients.Domain.ValueObjects;
using Patients.Infrastructure.Persistence;

namespace Patients.Infrastructure.Repositories;

public sealed class PatientRepository : IPatientRepository
{
    private readonly PatientsDbContext _context;

    public PatientRepository(PatientsDbContext context) => _context = context;

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> CpfExistsAsync(Guid organizationId, string cpf, CancellationToken ct = default)
    {
        var cpfResult = Cpf.Create(cpf);
        if (cpfResult.IsFailure)
            return Task.FromResult(false); // CPF mal formado nunca bate com um cadastro existente válido

        return _context.Patients
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — ver XML doc da interface
            .AnyAsync(p => p.OrganizationId == organizationId && p.Cpf == cpfResult.Value, ct);
    }

    public async Task AddAsync(Patient patient, CancellationToken ct = default)
        => await _context.Patients.AddAsync(patient, ct);

    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> ListAsync(
        string? nome,
        string? cpf,
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // Sem OrganizationId explícito: o filtro global do EF Core (ApplyOrganizationQueryFilters) já
        // restringe a query ao organization corrente via IOrganizationContext.
        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.Ativo);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            // ToLower().Contains() em vez de função específica do Postgres (ex: EF.Functions.ILike)
            // de propósito — precisa traduzir também no provider InMemory usado pelos testes.
            var nomeLower = nome.ToLower();
            query = query.Where(p => p.NomeCompleto.ToLower().Contains(nomeLower));
        }

        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var cpfResult = Cpf.Create(cpf);
            query = cpfResult.IsSuccess
                ? query.Where(p => p.Cpf == cpfResult.Value)
                : query.Where(p => false); // filtro de CPF mal formado nunca bate com cadastro válido
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.NomeCompleto)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
