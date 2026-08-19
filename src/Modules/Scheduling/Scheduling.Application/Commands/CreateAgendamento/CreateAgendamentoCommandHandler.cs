using MediatR;
using Patients.Contracts;
using Scheduling.Application.Caching;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.CreateAgendamento;

/// <summary>
/// Mesmo lock curto no Redis usado em ConfirmarAgendamentoCommandHandler (docs/decisions.md) —
/// sem ele, duas requisições concorrentes pro MESMO profissional/horário podiam ambas ler "sem
/// sobreposição" (via ExisteSobreposicaoAsync) antes de qualquer uma commitar, e as duas
/// persistiam como Agendado sobrepostos: são dois INSERTs distintos, o token otimista (xmin) não
/// protege nada aí (só pega conflito de UPDATE na mesma linha). Fix (QA/jubileu, IMP-1):
/// valida existência de paciente/profissional/sala ANTES do lock (checagem barata, não precisa
/// de seção crítica); adquire o lock; REVALIDA ExisteSobreposicaoAsync depois de tê-lo; só então
/// cria e salva. Chave de lock é por minuto exato (yyyyMMddHHmm) — não pega sobreposição parcial
/// em minutos diferentes (ex: 09:00-09:30 vs 09:15-09:45) sozinha, mas quem garante a invariante
/// é a query de sobreposição DENTRO do lock, não o lock em si (o lock só serializa a corrida no
/// caso mais comum de disputa pelo mesmo slot exato).
/// </summary>
public sealed class CreateAgendamentoCommandHandler : IRequestHandler<CreateAgendamentoCommand, Result<AgendamentoDto>>
{
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly ISalaRepository _salaRepository;
    private readonly IPatientLookup _patientLookup;
    private readonly IRedisLockService _lockService;
    private readonly IAvailabilityCache _availabilityCache;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepository,
        IProfissionalRepository profissionalRepository,
        ISalaRepository salaRepository,
        IPatientLookup patientLookup,
        IRedisLockService lockService,
        IAvailabilityCache availabilityCache,
        IUnitOfWork unitOfWork)
    {
        _agendamentoRepository = agendamentoRepository;
        _profissionalRepository = profissionalRepository;
        _salaRepository = salaRepository;
        _patientLookup = patientLookup;
        _lockService = lockService;
        _availabilityCache = availabilityCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AgendamentoDto>> Handle(CreateAgendamentoCommand request, CancellationToken cancellationToken)
    {
        if (!await _patientLookup.ExistsAsync(request.PacienteId, cancellationToken))
            return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.PacienteNaoEncontrado);

        var profissional = await _profissionalRepository.GetByIdAsync(request.ProfissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo)
            return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.ProfissionalNaoEncontrado);

        var sala = await _salaRepository.GetByIdAsync(request.SalaId, cancellationToken);
        if (sala is null || !sala.Ativa)
            return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.SalaNaoEncontrada);

        var lockKey = SchedulingCacheKeys.LockConfirmacao(request.OrganizationId, request.ProfissionalId, request.Inicio);
        var lockToken = await _lockService.AcquireAsync(lockKey, TimeSpan.FromSeconds(5), cancellationToken);
        if (lockToken is null)
            return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.LockIndisponivel);

        try
        {
            // Revalida DEPOIS de ter o lock — é a query, não o lock, que garante a invariante de
            // não-sobreposição (ver XML doc da classe).
            if (await _agendamentoRepository.ExisteSobreposicaoAsync(request.ProfissionalId, request.Inicio, request.Fim, ignorarAgendamentoId: null, cancellationToken))
                return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.HorarioIndisponivel);

            var agendamentoResult = Agendamento.Criar(request.OrganizationId, request.PacienteId, request.ProfissionalId, request.SalaId, request.Inicio, request.Fim);
            if (agendamentoResult.IsFailure)
                return Result.Failure<AgendamentoDto>(agendamentoResult.Error);

            var agendamento = agendamentoResult.Value;

            await _agendamentoRepository.AddAsync(agendamento, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Slot que era livre agora está ocupado — cache de disponibilidade daquele dia fica desatualizado.
            await _availabilityCache.RemoveAsync(
                SchedulingCacheKeys.Disponibilidade(request.OrganizationId, request.ProfissionalId, DateOnly.FromDateTime(request.Inicio)),
                cancellationToken);

            return Result.Success(agendamento.ToDto());
        }
        finally
        {
            await _lockService.ReleaseAsync(lockKey, lockToken, cancellationToken);
        }
    }
}
