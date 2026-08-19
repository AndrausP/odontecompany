using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.CreateConvenio;

public sealed record CreateConvenioCommand(Guid OrganizationId, string Nome, string? CodigoExterno) : IRequest<Result<ConvenioDto>>;
