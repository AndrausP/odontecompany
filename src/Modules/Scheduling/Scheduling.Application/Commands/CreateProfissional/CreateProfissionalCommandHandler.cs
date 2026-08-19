using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Errors;
using SharedKernel;
using Tenancy.Contracts;

namespace Scheduling.Application.Commands.CreateProfissional;

/// <summary>
/// Cria um profissional. Se <see cref="CreateProfissionalCommand.BranchId"/> for informado,
/// valida via <c>Tenancy.Contracts.IBranchLookup</c> (nunca Tenancy.Domain/Infrastructure) —
/// mesma fronteira cross-module já usada com Patients.Contracts.IPatientLookup.
/// </summary>
public sealed class CreateProfissionalCommandHandler : IRequestHandler<CreateProfissionalCommand, Result<ProfissionalDto>>
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IBranchLookup _branchLookup;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProfissionalCommandHandler(IProfissionalRepository profissionalRepository, IBranchLookup branchLookup, IUnitOfWork unitOfWork)
    {
        _profissionalRepository = profissionalRepository;
        _branchLookup = branchLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfissionalDto>> Handle(CreateProfissionalCommand request, CancellationToken cancellationToken)
    {
        if (request.BranchId is not null && !await _branchLookup.ExistsAsync(request.OrganizationId, request.BranchId.Value, cancellationToken))
            return Result.Failure<ProfissionalDto>(DomainErrors.Profissional.BranchInvalida);

        var profissionalResult = Profissional.Criar(request.OrganizationId, request.Nome, request.Especialidade, request.UserId, request.BranchId);
        if (profissionalResult.IsFailure)
            return Result.Failure<ProfissionalDto>(profissionalResult.Error);

        var profissional = profissionalResult.Value;

        await _profissionalRepository.AddAsync(profissional, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(profissional.ToDto());
    }
}
