using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.CreateFaturaFromConsultaConcluida;

/// <summary>
/// Espelha o payload de <c>Scheduling.Contracts.ConsultaConcluidaEvent</c> — não referencia
/// Scheduling.Contracts diretamente pra não acoplar Billing.Application a outro módulo além do
/// necessário (Patients.Contracts, já usado nos demais commands). Quem traduz o evento pra este
/// command é a camada de mensageria em Billing.Infrastructure (consumidor MassTransit, quando
/// existir) ou, hoje, um chamador manual/reconciliação via API.
/// </summary>
public sealed record CreateFaturaFromConsultaConcluidaCommand(
    Guid AgendamentoId,
    Guid OrganizationId,
    Guid PacienteId,
    Guid ProfissionalId,
    DateTime DataHoraConclusao,
    decimal Valor
) : IRequest<Result<Guid>>;
