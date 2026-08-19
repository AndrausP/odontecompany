using Contracts.Abstractions;

namespace Scheduling.Contracts;

/// <summary>
/// Contrato público do módulo Scheduling. Status vem como string (não o enum de domínio) de
/// propósito — mesmo padrão de <c>Identity.Contracts.ICurrentUserAccessor.Role</c>: consumidores
/// externos ao módulo nunca enxergam <c>Scheduling.Domain.Enums.AgendamentoStatus</c>, só o
/// próprio módulo Scheduling (Application/Infrastructure) tem essa dependência. Mapeamento
/// Agendamento (entidade) → AgendamentoDto mora em Scheduling.Application (AgendamentoMappingExtensions).
/// </summary>
public sealed record AgendamentoDto(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    Guid SalaId,
    DateTime Inicio,
    DateTime Fim,
    string Status,
    string? MotivoCancelamento,
    decimal? ValorConsulta,
    DateTime CreatedAt,
    DateTime? UpdatedAt
) : IModuleContract;
