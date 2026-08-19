using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.CancelarFatura;

public sealed record CancelarFaturaCommand(Guid FaturaId) : IRequest<Result>;
