namespace Scheduling.Application.Caching;

/// <summary>
/// Convenção única de chave de cache/lock do módulo Scheduling — usada tanto pelos handlers
/// (invalidação) quanto pelas implementações de <c>IAvailabilityCache</c>/<c>IRedisLockService</c>
/// (Infrastructure), pra nunca divergir formato de chave entre quem escreve e quem lê.
/// </summary>
public static class SchedulingCacheKeys
{
    /// <summary>Lista de slots livres (Contracts.Abstractions/decisions.md): scheduling:disponibilidade:{organizationId}:{profissionalId}:{data:yyyy-MM-dd}, TTL 1h.</summary>
    public static string Disponibilidade(Guid organizationId, Guid profissionalId, DateOnly data)
        => $"scheduling:disponibilidade:{organizationId}:{profissionalId}:{data:yyyy-MM-dd}";

    /// <summary>Lock curto de confirmação: scheduling:lock:{organizationId}:{profissionalId}:{inicio:yyyyMMddHHmm}, TTL 5s.</summary>
    public static string LockConfirmacao(Guid organizationId, Guid profissionalId, DateTime inicio)
        => $"scheduling:lock:{organizationId}:{profissionalId}:{inicio:yyyyMMddHHmm}";
}
