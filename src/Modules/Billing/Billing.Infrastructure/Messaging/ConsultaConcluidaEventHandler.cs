using Billing.Application.Commands.CreateFaturaFromConsultaConcluida;
using MediatR;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;

namespace Billing.Infrastructure.Messaging;

/// <summary>
/// Tradutor de <see cref="ConsultaConcluidaEvent"/> (contrato público do Scheduling,
/// task 004) pra <see cref="CreateFaturaFromConsultaConcluidaCommand"/> (Billing). Referencia
/// Scheduling.Contracts (nunca Scheduling.Domain/Infrastructure), mesmo padrão de fronteira
/// cross-module já usado em Scheduling→Patients.Contracts.
///
/// PENDÊNCIA CONHECIDA (ver docs/tasks/006-financeiro-particular.md): esta classe NÃO está
/// conectada a nenhum bus de mensagens de verdade. O worker que lê `SchedulingOutboxMessages` e
/// publica no RabbitMQ (via MassTransit) nunca foi implementado — isso já era dívida técnica
/// documentada desde a task 004 (Scheduling). Sem o worker, nenhum evento chega até aqui.
/// Esta classe existe pra que a TRADUÇÃO evento→comando já esteja pronta e testada; quando o
/// worker existir, um <c>IConsumer&lt;ConsultaConcluidaEvent&gt;</c> real do MassTransit só
/// precisa chamar <see cref="HandleAsync"/>. Até lá, o único jeito de faturar automaticamente
/// uma consulta concluída é via chamada direta/reconciliação manual
/// (`POST /api/faturas/from-consulta`, ver FaturasController).
/// </summary>
public sealed class ConsultaConcluidaEventHandler
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConsultaConcluidaEventHandler> _logger;

    public ConsultaConcluidaEventHandler(IMediator mediator, ILogger<ConsultaConcluidaEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(ConsultaConcluidaEvent evt, CancellationToken ct = default)
    {
        var command = new CreateFaturaFromConsultaConcluidaCommand(
            evt.AgendamentoId, evt.OrganizationId, evt.PacienteId, evt.ProfissionalId, evt.DataHoraConclusao, evt.Valor);

        var result = await _mediator.Send(command, ct);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Falha ao gerar fatura automática pro agendamento {AgendamentoId}: {Erro}",
                evt.AgendamentoId, result.Error.Message);
        }
    }
}
