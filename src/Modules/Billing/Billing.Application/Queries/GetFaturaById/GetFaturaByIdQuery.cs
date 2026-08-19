using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Billing.Application.Queries.GetFaturaById;

public sealed record GetFaturaByIdQuery(Guid Id) : IRequest<Result<FaturaDto>>;
