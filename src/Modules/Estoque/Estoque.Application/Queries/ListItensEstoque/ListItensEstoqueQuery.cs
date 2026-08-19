using Contracts.Abstractions.Pagination;
using Estoque.Contracts;
using MediatR;
using SharedKernel;

namespace Estoque.Application.Queries.ListItensEstoque;

public sealed record ListItensEstoqueQuery(PageRequest Pagination, Guid? BranchId, bool IncludeInactive) : IRequest<Result<PagedResult<ItemEstoqueDto>>>;
