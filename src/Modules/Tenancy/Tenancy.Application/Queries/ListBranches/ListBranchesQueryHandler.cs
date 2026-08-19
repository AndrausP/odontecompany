using MediatR;
using SharedKernel;
using Tenancy.Application.Interfaces;
using Tenancy.Application.Mapping;
using Tenancy.Contracts;

namespace Tenancy.Application.Queries.ListBranches;

public sealed class ListBranchesQueryHandler : IRequestHandler<ListBranchesQuery, Result<IReadOnlyList<BranchDto>>>
{
    private readonly IBranchRepository _branchRepository;

    public ListBranchesQueryHandler(IBranchRepository branchRepository) => _branchRepository = branchRepository;

    public async Task<Result<IReadOnlyList<BranchDto>>> Handle(ListBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.ListByOrganizationAsync(request.OrganizationId, request.IncludeInactive, cancellationToken);
        return Result.Success<IReadOnlyList<BranchDto>>(branches.Select(u => u.ToDto()).ToList());
    }
}
