using Patients.Domain.Entities;

namespace Patients.Application.Interfaces;

public interface IPatientRepository
{
    /// <summary>Busca por id. Escopado pelo filtro global de organization do DbContext — não precisa de OrganizationId explícito.</summary>
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checa unicidade de CPF POR ORGANIZATION (não global — CPF pode repetir entre clínicas
    /// diferentes). Ignora o filtro global e recebe o organization explicitamente pra não depender
    /// do IOrganizationContext ambiente estar resolvido igual ao organization informado — método
    /// autocontido, fácil de testar isoladamente.
    /// </summary>
    Task<bool> CpfExistsAsync(Guid organizationId, string cpf, CancellationToken ct = default);

    Task AddAsync(Patient patient, CancellationToken ct = default);

    /// <summary>Listagem paginada escopada pelo filtro global de organization. Nome faz busca parcial (contains); Cpf, exata.</summary>
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> ListAsync(
        string? nome,
        string? cpf,
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
