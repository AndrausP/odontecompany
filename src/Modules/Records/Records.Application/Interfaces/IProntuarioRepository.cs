using Records.Domain.Entities;

namespace Records.Application.Interfaces;

public interface IProntuarioRepository
{
    /// <summary>Busca por id, com evoluções e anexos carregados. Escopado pelo filtro global de organization.</summary>
    Task<Prontuario?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Busca o prontuário de um paciente (relação 1:1). Escopado pelo filtro global de organization.</summary>
    Task<Prontuario?> GetByPacienteIdAsync(Guid pacienteId, CancellationToken ct = default);

    Task<bool> ExistsByPacienteIdAsync(Guid pacienteId, CancellationToken ct = default);

    Task AddAsync(Prontuario prontuario, CancellationToken ct = default);

    Task AddEvolucaoAsync(EvolucaoClinica evolucao, CancellationToken ct = default);

    Task AddAnexoAsync(AnexoMetadata anexo, CancellationToken ct = default);

    Task<IReadOnlyList<EvolucaoClinica>> ListEvolucoesAsync(Guid prontuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<AnexoMetadata>> ListAnexosAsync(Guid prontuarioId, CancellationToken ct = default);
}
