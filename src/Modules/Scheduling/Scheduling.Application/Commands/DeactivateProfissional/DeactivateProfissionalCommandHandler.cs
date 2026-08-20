using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.DeactivateProfissional;

public sealed class DeactivateProfissionalCommandHandler : IRequestHandler<DeactivateProfissionalCommand, Result>
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProfissionalCommandHandler(IProfissionalRepository profissionalRepository, IUnitOfWork unitOfWork)
    {
        _profissionalRepository = profissionalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivateProfissionalCommand request, CancellationToken cancellationToken)
    {
        var profissional = await _profissionalRepository.GetByIdAsync(request.ProfissionalId, cancellationToken);
        if (profissional is null || profissional.OrganizationId != request.OrganizationId)
            return Result.Failure(DomainErrors.Profissional.NaoEncontrado);

        profissional.Desativar();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
