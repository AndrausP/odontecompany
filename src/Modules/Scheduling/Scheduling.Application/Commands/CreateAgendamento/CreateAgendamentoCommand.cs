using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.CreateAgendamento;

/// <summary>OrganizationId nunca vem do corpo da requisição HTTP — o controller monta este command com o organization_id extraído da claim do JWT (mesmo padrão de CreatePatientCommand).</summary>
public sealed record CreateAgendamentoCommand(
    Guid OrganizationId,
    Guid PacienteId,
    Guid ProfissionalId,
    Guid SalaId,
    DateTime Inicio,
    DateTime Fim,
    Guid? ProcedimentoId = null
) : IRequest<Result<AgendamentoDto>>;
