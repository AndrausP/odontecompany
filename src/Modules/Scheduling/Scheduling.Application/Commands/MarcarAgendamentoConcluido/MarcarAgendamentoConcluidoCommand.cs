using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.MarcarAgendamentoConcluido;

/// <summary>RequestingUserId/RequestingUserRole: ver XML doc de <c>ConfirmarAgendamentoCommand</c> — mesmo padrão.</summary>
public sealed record MarcarAgendamentoConcluidoCommand(
    Guid Id,
    decimal Valor,
    Guid? RequestingUserId = null,
    string? RequestingUserRole = null
) : IRequest<Result<AgendamentoDto>>;
