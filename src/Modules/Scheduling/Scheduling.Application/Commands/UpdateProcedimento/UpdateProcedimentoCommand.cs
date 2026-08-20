using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.UpdateProcedimento;

public sealed record UpdateProcedimentoCommand(Guid ProcedimentoId, Guid OrganizationId, string Nome, decimal? ValorPadrao, int? DuracaoPadraoMinutos)
    : IRequest<Result<ProcedimentoDto>>;
