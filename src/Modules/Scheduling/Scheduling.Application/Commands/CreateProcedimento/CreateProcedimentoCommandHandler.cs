using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Entities;
using SharedKernel;

namespace Scheduling.Application.Commands.CreateProcedimento;

public sealed class CreateProcedimentoCommandHandler : IRequestHandler<CreateProcedimentoCommand, Result<ProcedimentoDto>>
{
    private readonly IProcedimentoRepository _procedimentoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProcedimentoCommandHandler(IProcedimentoRepository procedimentoRepository, IUnitOfWork unitOfWork)
    {
        _procedimentoRepository = procedimentoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProcedimentoDto>> Handle(CreateProcedimentoCommand request, CancellationToken cancellationToken)
    {
        var procedimentoResult = Procedimento.Criar(request.OrganizationId, request.Nome, request.ValorPadrao, request.DuracaoPadraoMinutos);
        if (procedimentoResult.IsFailure)
            return Result.Failure<ProcedimentoDto>(procedimentoResult.Error);

        var procedimento = procedimentoResult.Value;

        await _procedimentoRepository.AddAsync(procedimento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(procedimento.ToDto());
    }
}
