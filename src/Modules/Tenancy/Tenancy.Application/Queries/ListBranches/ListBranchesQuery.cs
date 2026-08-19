using MediatR;
using SharedKernel;
using Tenancy.Contracts;

namespace Tenancy.Application.Queries.ListBranches;

public sealed record ListBranchesQuery(Guid OrganizationId, bool IncludeInactive) : IRequest<Result<IReadOnlyList<BranchDto>>>;
