using MediatR;
using Reporting.Contracts;
using SharedKernel;

namespace Reporting.Application.Queries.GetDashboardResumo;

public sealed record GetDashboardResumoQuery(Guid OrganizationId, DateTime DataInicio, DateTime DataFim) : IRequest<Result<DashboardResumoDto>>;
