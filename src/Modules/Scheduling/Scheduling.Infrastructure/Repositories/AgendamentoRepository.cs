using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Repositories;

public sealed class AgendamentoRepository : IAgendamentoRepository
{
    private readonly SchedulingDbContext _context;

    public AgendamentoRepository(SchedulingDbContext context) => _context = context;

    public Task<Agendamento?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Agendamentos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(Agendamento agendamento, CancellationToken ct = default)
        => await _context.Agendamentos.AddAsync(agendamento, ct);

    public Task<bool> ExisteSobreposicaoAsync(
        Guid profissionalId, DateTime inicio, DateTime fim, Guid? ignorarAgendamentoId, CancellationToken ct = default)
    {
        // Sobreposição de intervalos semi-abertos: A.Inicio < B.Fim && B.Inicio < A.Fim.
        // Só Agendado/Confirmado ocupam o horário — Cancelado/Concluido não bloqueiam mais nada.
        var query = _context.Agendamentos
            .Where(a => a.ProfissionalId == profissionalId)
            .Where(a => a.Status == AgendamentoStatus.Agendado || a.Status == AgendamentoStatus.Confirmado)
            .Where(a => a.Periodo.Inicio < fim && inicio < a.Periodo.Fim);

        if (ignorarAgendamentoId.HasValue)
            query = query.Where(a => a.Id != ignorarAgendamentoId.Value);

        return query.AnyAsync(ct);
    }

    public async Task<(IReadOnlyList<Agendamento> Items, int TotalCount)> ListAsync(
        Guid? profissionalId,
        Guid? pacienteId,
        AgendamentoStatus? status,
        DateTime? dataInicio,
        DateTime? dataFim,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Agendamentos.AsNoTracking().AsQueryable();

        if (profissionalId.HasValue)
            query = query.Where(a => a.ProfissionalId == profissionalId.Value);

        if (pacienteId.HasValue)
            query = query.Where(a => a.PacienteId == pacienteId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (dataInicio.HasValue)
            query = query.Where(a => a.Periodo.Inicio >= dataInicio.Value);

        if (dataFim.HasValue)
            query = query.Where(a => a.Periodo.Fim <= dataFim.Value);

        if (branchId.HasValue)
        {
            // Fase 5: restringe a agendamentos de profissionais lotados na branch informada —
            // subquery, não JOIN cruzado de módulo (Profissional é do próprio Scheduling).
            var profissionalIdsDaBranch = _context.Profissionais.AsNoTracking()
                .Where(p => p.BranchId == branchId.Value)
                .Select(p => p.Id);

            query = query.Where(a => profissionalIdsDaBranch.Contains(a.ProfissionalId));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.Periodo.Inicio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Agendamento>> ListByProfissionalAndDataAsync(Guid profissionalId, DateTime data, CancellationToken ct = default)
    {
        var inicioDoDia = data.Date;
        var fimDoDia = inicioDoDia.AddDays(1);

        return await _context.Agendamentos
            .AsNoTracking()
            .Where(a => a.ProfissionalId == profissionalId)
            .Where(a => a.Periodo.Inicio >= inicioDoDia && a.Periodo.Inicio < fimDoDia)
            .ToListAsync(ct);
    }
}
