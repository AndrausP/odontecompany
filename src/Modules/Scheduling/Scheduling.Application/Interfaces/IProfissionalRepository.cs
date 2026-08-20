using Scheduling.Domain.Entities;

namespace Scheduling.Application.Interfaces;

public interface IProfissionalRepository
{
    Task<Profissional?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Profissional profissional, CancellationToken ct = default);

    /// <summary>Listagem simples (sem paginação — volume esperado é baixo, dezenas por clínica) escopada pelo filtro global de organization.</summary>
    Task<IReadOnlyList<Profissional>> ListAsync(bool includeInactive, CancellationToken ct = default);

    /// <summary>
    /// Profissionais com este email ainda SEM usuário vinculado, em QUALQUER organization —
    /// task 042 (link automático ao aceitar convite). Ignora o filtro global de propósito: quem
    /// está aceitando o convite pode não ter esta organization como ativa no token ainda (mesma
    /// exceção documentada em outros repositórios "AcrossOrganizations" do projeto).
    /// </summary>
    Task<IReadOnlyList<Profissional>> ListPendentesPorEmailAcrossOrganizationsAsync(Guid organizationId, string email, CancellationToken ct = default);
}
