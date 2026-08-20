using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.CreateProcedimento;

public sealed record CreateProcedimentoCommand(Guid OrganizationId, string Nome, decimal? ValorPadrao, int? DuracaoPadraoMinutos)
    : IRequest<Result<ProcedimentoDto>>;
