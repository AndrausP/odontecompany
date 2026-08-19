using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Reporting.Application.Queries.GetFaturamentoPorPeriodo;

public sealed record GetFaturamentoPorPeriodoQuery(Guid OrganizationId, DateTime DataInicio, DateTime DataFim) : IRequest<Result<FaturamentoResumoDto>>;
