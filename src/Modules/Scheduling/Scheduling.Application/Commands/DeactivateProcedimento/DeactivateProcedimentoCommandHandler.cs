using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.DeactivateProcedimento;

public sealed class DeactivateProcedimentoCommandHandler : IRequestHandler<DeactivateProcedimentoCommand, Result>
{
    private readonly IProcedimentoRepository _procedimentoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProcedimentoCommandHandler(IProcedimentoRepository procedimentoRepository, IUnitOfWork unitOfWork)
    {
        _procedimentoRepository = procedimentoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivateProcedimentoCommand request, CancellationToken cancellationToken)
    {
        var procedimento = await _procedimentoRepository.GetByIdAsync(request.ProcedimentoId, cancellationToken);
        if (procedimento is null || procedimento.OrganizationId != request.OrganizationId)
            return Result.Failure(DomainErrors.Procedimento.NaoEncontrado);

        procedimento.Desativar();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
