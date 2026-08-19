using MediatR;
using Scheduling.Application.Authorization;
using Scheduling.Application.Caching;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.CancelarAgendamento;

public sealed class CancelarAgendamentoCommandHandler : IRequestHandler<CancelarAgendamentoCommand, Result>
{
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IAvailabilityCache _availabilityCache;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepository,
        IProfissionalRepository profissionalRepository,
        IAvailabilityCache availabilityCache,
        IUnitOfWork unitOfWork)
    {
        _agendamentoRepository = agendamentoRepository;
        _profissionalRepository = profissionalRepository;
        _availabilityCache = availabilityCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelarAgendamentoCommand request, CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoRepository.GetByIdAsync(request.Id, cancellationToken);
        if (agendamento is null)
            return Result.Failure(DomainErrors.Agendamento.NaoEncontrado);

        var ownershipResult = await AgendaOwnershipGuard.EnsurePodeGerenciarAsync(
            _profissionalRepository, agendamento.ProfissionalId, request.RequestingUserId, request.RequestingUserRole, cancellationToken);
        if (ownershipResult.IsFailure)
            return ownershipResult;

        var cancelarResult = agendamento.Cancelar(request.Motivo);
        if (cancelarResult.IsFailure)
            return cancelarResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Slot que estava ocupado volta a ficar livre — cache de disponibilidade daquele dia fica desatualizado.
        await _availabilityCache.RemoveAsync(
            SchedulingCacheKeys.Disponibilidade(agendamento.OrganizationId, agendamento.ProfissionalId, DateOnly.FromDateTime(agendamento.Periodo.Inicio)),
            cancellationToken);

        return Result.Success();
    }
}
