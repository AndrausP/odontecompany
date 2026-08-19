using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.ConfirmarAgendamento;

/// <summary>
/// RequestingUserId/RequestingUserRole nunca vêm do corpo da requisição HTTP — o controller monta
/// este command com os dados extraídos da claim do JWT do usuário autenticado (mesmo padrão de
/// OrganizationId em CreateAgendamentoCommand). Usados só pra checagem de ownership de agenda
/// (Dentista só mexe na própria) — ver <see cref="Scheduling.Application.Authorization.AgendaOwnershipGuard"/>.
/// </summary>
public sealed record ConfirmarAgendamentoCommand(
    Guid Id,
    Guid? RequestingUserId = null,
    string? RequestingUserRole = null
) : IRequest<Result<AgendamentoDto>>;
