using MediatR;
using Scheduling.Contracts;
using Scheduling.Domain.Enums;
using SharedKernel;

namespace Scheduling.Application.Commands.UpdateProfissional;

/// <summary>Tela de Configurações — edita os campos que a criação já aceitava. Não mexe em Email
/// como "gatilho de convite" (isso só acontece na criação, via orquestração do controller) —
/// aqui o Email só é dado de contato/pareamento pra um convite futuro.</summary>
public sealed record UpdateProfissionalCommand(
    Guid ProfissionalId,
    Guid OrganizationId,
    string Nome,
    string Especialidade,
    TipoContrato TipoContrato,
    decimal? PercentualComissaoDefault,
    string? Email,
    Guid? BranchId
) : IRequest<Result<ProfissionalDto>>;
