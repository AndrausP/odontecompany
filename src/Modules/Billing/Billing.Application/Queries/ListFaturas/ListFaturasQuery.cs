using Billing.Contracts;
using Contracts.Abstractions.Pagination;
using MediatR;
using SharedKernel;

namespace Billing.Application.Queries.ListFaturas;

public sealed record ListFaturasQuery(PageRequest Pagination, Guid? PacienteId, string? Status) : IRequest<Result<PagedResult<FaturaDto>>>;
