using Microsoft.EntityFrameworkCore;
using Records.Application.Interfaces;
using Records.Domain.Entities;
using Records.Infrastructure.Persistence;

namespace Records.Infrastructure.Repositories;

public sealed class ProntuarioRepository : IProntuarioRepository
{
    private readonly RecordsDbContext _context;

    public ProntuarioRepository(RecordsDbContext context) => _context = context;

    public Task<Prontuario?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Prontuarios.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Prontuario?> GetByPacienteIdAsync(Guid pacienteId, CancellationToken ct = default)
        => _context.Prontuarios.FirstOrDefaultAsync(p => p.PacienteId == pacienteId, ct);

    public Task<bool> ExistsByPacienteIdAsync(Guid pacienteId, CancellationToken ct = default)
        => _context.Prontuarios.AnyAsync(p => p.PacienteId == pacienteId, ct);

    public async Task AddAsync(Prontuario prontuario, CancellationToken ct = default)
        => await _context.Prontuarios.AddAsync(prontuario, ct);

    public async Task AddEvolucaoAsync(EvolucaoClinica evolucao, CancellationToken ct = default)
        => await _context.EvolucoesClinicas.AddAsync(evolucao, ct);

    public async Task AddAnexoAsync(AnexoMetadata anexo, CancellationToken ct = default)
        => await _context.AnexosMetadata.AddAsync(anexo, ct);

    public async Task<IReadOnlyList<EvolucaoClinica>> ListEvolucoesAsync(Guid prontuarioId, CancellationToken ct = default)
        => await _context.EvolucoesClinicas
            .Where(e => e.ProntuarioId == prontuarioId)
            .OrderByDescending(e => e.DataRegistro)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AnexoMetadata>> ListAnexosAsync(Guid prontuarioId, CancellationToken ct = default)
        => await _context.AnexosMetadata
            .Where(a => a.ProntuarioId == prontuarioId)
            .OrderByDescending(a => a.DataUpload)
            .ToListAsync(ct);
}
