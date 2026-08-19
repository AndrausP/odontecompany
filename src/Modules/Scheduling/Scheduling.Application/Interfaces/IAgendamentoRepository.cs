using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.Application.Interfaces;

public interface IAgendamentoRepository
{
    /// <summary>Busca por id, COM tracking (o caller pode precisar mutar e salvar em seguida). Escopado pelo filtro global de organization.</summary>
    Task<Agendamento?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Agendamento agendamento, CancellationToken ct = default);

    /// <summary>
    /// Existe outro agendamento Agendado/Confirmado do mesmo profissional cujo período sobrepõe
    /// [inicio, fim)? <paramref name="ignorarAgendamentoId"/> exclui o próprio agendamento da
    /// checagem (uso: revalidar sobreposição ao confirmar um agendamento já existente).
    /// </summary>
    Task<bool> ExisteSobreposicaoAsync(
        Guid profissionalId, DateTime inicio, DateTime fim, Guid? ignorarAgendamentoId, CancellationToken ct = default);

    /// <summary>
    /// Listagem paginada escopada pelo filtro global de organization, com filtros opcionais.
    /// <paramref name="branchId"/> (Fase 5) filtra pra só agendamentos de profissionais
    /// lotados naquela branch — usado pelo controller pra forçar o escopo de Recepcao (não é
    /// opcional pro chamador burlar, é decidido a partir de <c>ICurrentUserAccessor</c>).
    /// </summary>
    Task<(IReadOnlyList<Agendamento> Items, int TotalCount)> ListAsync(
        Guid? profissionalId,
        Guid? pacienteId,
        AgendamentoStatus? status,
        DateTime? dataInicio,
        DateTime? dataFim,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Todos os agendamentos de um profissional num dia específico — usado pra calcular gaps livres de disponibilidade.</summary>
    Task<IReadOnlyList<Agendamento>> ListByProfissionalAndDataAsync(Guid profissionalId, DateTime data, CancellationToken ct = default);
}
