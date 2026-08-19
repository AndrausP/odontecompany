using Billing.Contracts;
using Billing.Domain.Enums;
using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.CreateFaturaParticular;

public sealed record CreateFaturaParticularCommand(
    Guid OrganizationId,
    Guid PacienteId,
    Guid? AgendamentoId,
    Guid? ProfissionalId,
    decimal ValorTotal,
    int NumeroParcelas,
    FormaPagamento FormaPagamento,
    decimal? ComissaoDentistaPercentual
) : IRequest<Result<FaturaDto>>;
