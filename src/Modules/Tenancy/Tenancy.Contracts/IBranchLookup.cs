namespace Tenancy.Contracts;

/// <summary>
/// Porta pública do módulo Tenancy pra outros módulos (Scheduling, Identity, Estoque)
/// validarem `BranchId` sem depender de Tenancy.Domain/Infrastructure — mesmo padrão de
/// `Patients.Contracts.IPatientLookup`.
/// </summary>
public interface IBranchLookup
{
    /// <summary>Existe branch ATIVA com este Id, pertencente a este organization?</summary>
    Task<bool> ExistsAsync(Guid organizationId, Guid branchId, CancellationToken ct = default);

    /// <summary>
    /// Nome de toda branch do organization (BranchId → Nome), batch, sem N+1 — INCLUI inativas
    /// (task 022: filial derivada de Profissional.BranchId não pode virar "id desconhecido" no
    /// relatório de comissão só porque a branch foi desativada depois).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ListarNomesAsync(Guid organizationId, CancellationToken ct = default);
}
