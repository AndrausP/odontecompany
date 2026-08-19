using MediatR;
using Scheduling.Application.Caching;
using Scheduling.Application.Interfaces;
using Scheduling.Contracts;
using Scheduling.Domain.Enums;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Queries.ListarDisponibilidade;

/// <summary>
/// Cache-aside (docs/decisions.md): miss calcula os gaps livres entre os agendamentos do dia
/// (Agendado/Confirmado — Cancelado/Concluido não ocupam slot) dentro do expediente
/// 08:00–18:00 (decisão do Dev Backend — spec não define horário de expediente; ajustar quando o
/// módulo de configuração de clínica existir) e seta cache com TTL 1h.
/// </summary>
public sealed class ListarDisponibilidadeQueryHandler : IRequestHandler<ListarDisponibilidadeQuery, Result<IReadOnlyList<AvailabilitySlotDto>>>
{
    private static readonly TimeOnly ExpedienteInicio = new(8, 0);
    private static readonly TimeOnly ExpedienteFim = new(18, 0);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAvailabilityCache _availabilityCache;

    public ListarDisponibilidadeQueryHandler(
        IProfissionalRepository profissionalRepository, IAgendamentoRepository agendamentoRepository, IAvailabilityCache availabilityCache)
    {
        _profissionalRepository = profissionalRepository;
        _agendamentoRepository = agendamentoRepository;
        _availabilityCache = availabilityCache;
    }

    public async Task<Result<IReadOnlyList<AvailabilitySlotDto>>> Handle(ListarDisponibilidadeQuery request, CancellationToken cancellationToken)
    {
        var profissional = await _profissionalRepository.GetByIdAsync(request.ProfissionalId, cancellationToken);
        if (profissional is null)
            return Result.Failure<IReadOnlyList<AvailabilitySlotDto>>(DomainErrors.Profissional.NaoEncontrado);

        var cacheKey = SchedulingCacheKeys.Disponibilidade(request.OrganizationId, request.ProfissionalId, request.Data);

        var cached = await _availabilityCache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var slots = await CalcularGapsLivresAsync(request.ProfissionalId, request.Data, cancellationToken);

        await _availabilityCache.SetAsync(cacheKey, slots, CacheTtl, cancellationToken);

        return Result.Success(slots);
    }

    private async Task<IReadOnlyList<AvailabilitySlotDto>> CalcularGapsLivresAsync(Guid profissionalId, DateOnly data, CancellationToken cancellationToken)
    {
        var agendamentosDoDia = await _agendamentoRepository.ListByProfissionalAndDataAsync(
            profissionalId, data.ToDateTime(TimeOnly.MinValue), cancellationToken);

        var ocupados = agendamentosDoDia
            .Where(a => a.Status is AgendamentoStatus.Agendado or AgendamentoStatus.Confirmado)
            .OrderBy(a => a.Periodo.Inicio)
            .ToList();

        var expedienteInicio = data.ToDateTime(ExpedienteInicio);
        var expedienteFim = data.ToDateTime(ExpedienteFim);

        var slots = new List<AvailabilitySlotDto>();
        var cursor = expedienteInicio;

        foreach (var agendamento in ocupados)
        {
            if (agendamento.Periodo.Inicio > cursor)
                slots.Add(new AvailabilitySlotDto(cursor, agendamento.Periodo.Inicio));

            if (agendamento.Periodo.Fim > cursor)
                cursor = agendamento.Periodo.Fim;
        }

        if (cursor < expedienteFim)
            slots.Add(new AvailabilitySlotDto(cursor, expedienteFim));

        return slots;
    }
}
