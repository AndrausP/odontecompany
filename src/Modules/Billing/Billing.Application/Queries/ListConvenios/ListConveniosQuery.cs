using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Billing.Application.Queries.ListConvenios;

public sealed record ListConveniosQuery(bool IncludeInactive) : IRequest<Result<IReadOnlyList<ConvenioDto>>>;
