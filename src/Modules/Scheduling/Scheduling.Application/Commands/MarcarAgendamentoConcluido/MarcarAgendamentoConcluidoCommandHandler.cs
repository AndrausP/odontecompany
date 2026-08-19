using MediatR;
using Scheduling.Application.Authorization;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.MarcarAgendamentoConcluido;

/// <summary>
/// Marca o agendamento como Concluido e dispara <c>ConsultaConcluidaEvent</c> — a entry de Outbox
/// é gravada por <c>SchedulingDbContext.SaveChangesAsync</c> NA MESMA transação deste
/// SaveChangesAsync (não aqui na Application; ver XML doc do DbContext). Não invalida cache de
/// disponibilidade: consulta concluída não volta a aparecer como slot livre do dia — o dia já
/// passou/está acontecendo, e o slot ocupado por uma consulta concluída não deve reabrir pra
/// outro agendamento.
/// </summary>
public sealed class MarcarAgendamentoConcluidoCommandHandler : IRequestHandler<MarcarAgendamentoConcluidoCommand, Result<AgendamentoDto>>
{
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarcarAgendamentoConcluidoCommandHandler(
        IAgendamentoRepository agendamentoRepository, IProfissionalRepository profissionalRepository, IUnitOfWork unitOfWork)
    {
        _agendamentoRepository = agendamentoRepository;
        _profissionalRepository = profissionalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AgendamentoDto>> Handle(MarcarAgendamentoConcluidoCommand request, CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoRepository.GetByIdAsync(request.Id, cancellationToken);
        if (agendamento is null)
            return Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.NaoEncontrado);

        var ownershipResult = await AgendaOwnershipGuard.EnsurePodeGerenciarAsync(
            _profissionalRepository, agendamento.ProfissionalId, request.RequestingUserId, request.RequestingUserRole, cancellationToken);
        if (ownershipResult.IsFailure)
            return Result.Failure<AgendamentoDto>(ownershipResult.Error);

        var concluirResult = agendamento.MarcarConcluido(request.Valor);
        if (concluirResult.IsFailure)
            return Result.Failure<AgendamentoDto>(concluirResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(agendamento.ToDto());
    }
}
