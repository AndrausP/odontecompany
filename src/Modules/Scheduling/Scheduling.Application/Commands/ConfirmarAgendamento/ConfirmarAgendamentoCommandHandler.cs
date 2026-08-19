using MediatR;
using Scheduling.Application.Authorization;
using Scheduling.Application.Caching;
using Scheduling.Application.Exceptions;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.ConfirmarAgendamento;

/// <summary>
/// Confirmação em dois tempos (registrado em docs/decisions.md): lock curto no Redis (5s) protege
/// a corrida entre requisições concorrentes confirmando o MESMO slot antes de qualquer commit;
/// o token otimista (xmin) é a segunda linha de defesa, mais barata, contra o que sobrar (ex: TTL
/// do lock expirou no meio de uma requisição lenta) — daí capturar <see cref="ConcurrencyConflictException"/>
/// mesmo já tendo o lock.
/// </summary>
public sealed class ConfirmarAgendamentoCommandHandler : IRequestHandler<ConfirmarAgendamentoCommand, Result<AgendamentoDto>>
{
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IRedisLockService _lockService;
    private readonly IAvailabilityCache _availabilityCache;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmarAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepository,
        IProfissionalRepository profissionalRepository,
        IRedisLockService lockService,
        IAvailabilityCache availabilityCache,
        IUnitOfWork unitOfWork)
    {
        _agendamentoRepository = agendamentoRepository;
        _profissionalRepository = profissionalRepository;
        _lockService = lockService;
        _availabilityCache = availabilityCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AgendamentoDto>> Handle(ConfirmarAgendamentoCommand request, CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoRepository.GetByIdAsync(request.Id, cancellationToken);
        if (agendamento is null)
            return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.NaoEncontrado);

        var ownershipResult = await AgendaOwnershipGuard.EnsurePodeGerenciarAsync(
            _profissionalRepository, agendamento.ProfissionalId, request.RequestingUserId, request.RequestingUserRole, cancellationToken);
        if (ownershipResult.IsFailure)
            return Result.Failure<AgendamentoDto>(ownershipResult.Error);

        var lockKey = SchedulingCacheKeys.LockConfirmacao(agendamento.OrganizationId, agendamento.ProfissionalId, agendamento.Periodo.Inicio);
        var lockToken = await _lockService.AcquireAsync(lockKey, TimeSpan.FromSeconds(5), cancellationToken);
        if (lockToken is null)
            return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.LockIndisponivel);

        try
        {
            // Revalida sobreposição DEPOIS de ter o lock — outra requisição pode ter confirmado
            // um agendamento conflitante enquanto esta esperava a checagem inicial (feita na
            // criação, sem lock nenhum).
            var sobrepoe = await _agendamentoRepository.ExisteSobreposicaoAsync(
                agendamento.ProfissionalId, agendamento.Periodo.Inicio, agendamento.Periodo.Fim, agendamento.Id, cancellationToken);

            if (sobrepoe)
                return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.HorarioIndisponivel);

            var confirmarResult = agendamento.Confirmar();
            if (confirmarResult.IsFailure)
                return Result.Failure<AgendamentoDto>(confirmarResult.Error);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.ConflitoDeConcorrencia);
            }

            await _availabilityCache.RemoveAsync(
                SchedulingCacheKeys.Disponibilidade(agendamento.OrganizationId, agendamento.ProfissionalId, DateOnly.FromDateTime(agendamento.Periodo.Inicio)),
                cancellationToken);

            return Result.Success(agendamento.ToDto());
        }
        finally
        {
            await _lockService.ReleaseAsync(lockKey, lockToken, cancellationToken);
        }
    }
}
