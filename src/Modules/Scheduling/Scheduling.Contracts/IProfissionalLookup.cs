namespace Scheduling.Contracts;

/// <summary>
/// Porta pública do módulo Scheduling pra outros módulos (Reporting, task 023) lerem
/// Profissional sem depender de Scheduling.Domain/Infrastructure — mesmo padrão de
/// <c>Tenancy.Contracts.IBranchLookup</c>. INCLUI profissionais inativos: comissão de
/// profissional já desligado ainda precisa aparecer em período passado (task 022).
/// </summary>
public interface IProfissionalLookup
{
    /// <summary>Todos os profissionais do organization — ativos e inativos, batch, sem N+1.</summary>
    Task<IReadOnlyList<ProfissionalResumoDto>> ListarPorOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}

public sealed record ProfissionalResumoDto(Guid Id, string Nome, Guid? UserId, Guid? BranchId, bool Ativo);
