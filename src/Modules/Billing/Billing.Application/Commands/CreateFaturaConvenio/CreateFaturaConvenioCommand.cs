using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.CreateFaturaConvenio;

public sealed record CreateFaturaConvenioCommand(
    Guid OrganizationId,
    Guid PacienteId,
    Guid ConvenioId,
    Guid? AgendamentoId,
    Guid? ProfissionalId,
    decimal ValorTotal,
    decimal? ComissaoDentistaPercentual
) : IRequest<Result<FaturaDto>>;
