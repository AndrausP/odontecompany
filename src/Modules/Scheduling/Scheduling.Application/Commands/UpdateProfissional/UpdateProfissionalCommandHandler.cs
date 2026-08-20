using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Commands.UpdateProfissional;

public sealed class UpdateProfissionalCommandHandler : IRequestHandler<UpdateProfissionalCommand, Result<ProfissionalDto>>
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfissionalCommandHandler(IProfissionalRepository profissionalRepository, IUnitOfWork unitOfWork)
    {
        _profissionalRepository = profissionalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfissionalDto>> Handle(UpdateProfissionalCommand request, CancellationToken cancellationToken)
    {
        var profissional = await _profissionalRepository.GetByIdAsync(request.ProfissionalId, cancellationToken);

        // Mesmo retorno pra "não existe" E "existe em outra organization" — mesma defesa contra
        // IDOR já usada em UpdateBranchCommandHandler/DeactivateBranchCommandHandler (task 040).
        if (profissional is null || profissional.OrganizationId != request.OrganizationId)
            return Result.Failure<ProfissionalDto>(DomainErrors.Profissional.NaoEncontrado);

        var updateResult = profissional.AtualizarDados(
            request.Nome, request.Especialidade, request.TipoContrato, request.PercentualComissaoDefault, request.Email, request.BranchId);
        if (updateResult.IsFailure)
            return Result.Failure<ProfissionalDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(profissional.ToDto());
    }
}
