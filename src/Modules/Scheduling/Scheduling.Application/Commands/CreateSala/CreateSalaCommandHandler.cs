using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Errors;
using SharedKernel;
using Tenancy.Contracts;

namespace Scheduling.Application.Commands.CreateSala;

public sealed class CreateSalaCommandHandler : IRequestHandler<CreateSalaCommand, Result<SalaDto>>
{
    private readonly ISalaRepository _salaRepository;
    private readonly IBranchLookup _branchLookup;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSalaCommandHandler(ISalaRepository salaRepository, IBranchLookup branchLookup, IUnitOfWork unitOfWork)
    {
        _salaRepository = salaRepository;
        _branchLookup = branchLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SalaDto>> Handle(CreateSalaCommand request, CancellationToken cancellationToken)
    {
        if (request.BranchId is not null && !await _branchLookup.ExistsAsync(request.OrganizationId, request.BranchId.Value, cancellationToken))
            return Result.Failure<SalaDto>(DomainErrors.Sala.BranchInvalida);

        var salaResult = Sala.Criar(request.OrganizationId, request.Nome, request.CapacidadeMaxima, request.BranchId);
        if (salaResult.IsFailure)
            return Result.Failure<SalaDto>(salaResult.Error);

        var sala = salaResult.Value;

        await _salaRepository.AddAsync(sala, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sala.ToDto());
    }
}
