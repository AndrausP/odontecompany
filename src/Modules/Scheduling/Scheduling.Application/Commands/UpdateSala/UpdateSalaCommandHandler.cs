using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.UpdateSala;

public sealed class UpdateSalaCommandHandler : IRequestHandler<UpdateSalaCommand, Result<SalaDto>>
{
    private readonly ISalaRepository _salaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalaCommandHandler(ISalaRepository salaRepository, IUnitOfWork unitOfWork)
    {
        _salaRepository = salaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SalaDto>> Handle(UpdateSalaCommand request, CancellationToken cancellationToken)
    {
        var sala = await _salaRepository.GetByIdAsync(request.SalaId, cancellationToken);
        if (sala is null || sala.OrganizationId != request.OrganizationId)
            return Result.Failure<SalaDto>(DomainErrors.Sala.NaoEncontrada);

        var updateResult = sala.AtualizarDados(request.Nome, request.CapacidadeMaxima, request.BranchId);
        if (updateResult.IsFailure)
            return Result.Failure<SalaDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sala.ToDto());
    }
}
