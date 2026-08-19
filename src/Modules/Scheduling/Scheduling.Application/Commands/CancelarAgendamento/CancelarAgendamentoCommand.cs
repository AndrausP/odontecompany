using MediatR;
using SharedKernel;

namespace Scheduling.Application.Commands.CancelarAgendamento;

/// <summary>RequestingUserId/RequestingUserRole: ver XML doc de <c>ConfirmarAgendamentoCommand</c> — mesmo padrão.</summary>
public sealed record CancelarAgendamentoCommand(
    Guid Id,
    string? Motivo,
    Guid? RequestingUserId = null,
    string? RequestingUserRole = null
) : IRequest<Result>;
