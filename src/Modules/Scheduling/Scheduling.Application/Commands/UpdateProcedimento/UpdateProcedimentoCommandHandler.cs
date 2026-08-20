using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.UpdateProcedimento;

public sealed class UpdateProcedimentoCommandHandler : IRequestHandler<UpdateProcedimentoCommand, Result<ProcedimentoDto>>
{
    private readonly IProcedimentoRepository _procedimentoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProcedimentoCommandHandler(IProcedimentoRepository procedimentoRepository, IUnitOfWork unitOfWork)
    {
        _procedimentoRepository = procedimentoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProcedimentoDto>> Handle(UpdateProcedimentoCommand request, CancellationToken cancellationToken)
    {
        var procedimento = await _procedimentoRepository.GetByIdAsync(request.ProcedimentoId, cancellationToken);
        if (procedimento is null || procedimento.OrganizationId != request.OrganizationId)
            return Result.Failure<ProcedimentoDto>(DomainErrors.Procedimento.NaoEncontrado);

        var updateResult = procedimento.AtualizarDados(request.Nome, request.ValorPadrao, request.DuracaoPadraoMinutos);
        if (updateResult.IsFailure)
            return Result.Failure<ProcedimentoDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(procedimento.ToDto());
    }
}
