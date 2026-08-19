using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.RegistrarPagamentoParcela;

public sealed record RegistrarPagamentoParcelaCommand(Guid FaturaId, Guid ParcelaId) : IRequest<Result<FaturaDto>>;
