using MediatR;
using Scheduling.Contracts;
using Scheduling.Domain.Enums;
using SharedKernel;

namespace Scheduling.Application.Commands.CreateProfissional;

/// <summary>
/// <see cref="Email"/> opcional (task 042) — se informado, o controller (Bootstrap) dispara um
/// convite Role.Dentista pra esse email logo após a criação. Sem email, o profissional já é
/// utilizável na Agenda como recurso puro (sem login), igual sempre foi.
/// </summary>
public sealed record CreateProfissionalCommand(
    Guid OrganizationId,
    string Nome,
    string Especialidade,
    TipoContrato TipoContrato,
    decimal? PercentualComissaoDefault,
    string? Email,
    Guid? UserId,
    Guid? BranchId
) : IRequest<Result<ProfissionalDto>>;
