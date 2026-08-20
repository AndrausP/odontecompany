using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.DeactivateSala;

public sealed class DeactivateSalaCommandHandler : IRequestHandler<DeactivateSalaCommand, Result>
{
    private readonly ISalaRepository _salaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateSalaCommandHandler(ISalaRepository salaRepository, IUnitOfWork unitOfWork)
    {
        _salaRepository = salaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivateSalaCommand request, CancellationToken cancellationToken)
    {
        var sala = await _salaRepository.GetByIdAsync(request.SalaId, cancellationToken);
        if (sala is null || sala.OrganizationId != request.OrganizationId)
            return Result.Failure(DomainErrors.Sala.NaoEncontrada);

        sala.Desativar();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
