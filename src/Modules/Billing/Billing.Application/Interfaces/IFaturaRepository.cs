using Billing.Domain.Entities;

namespace Billing.Application.Interfaces;

public interface IFaturaRepository
{
    Task<Fatura?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Checa idempotência: já existe fatura gerada pra este agendamento? Evita faturar a mesma consulta duas vezes.</summary>
    Task<bool> ExistsByAgendamentoIdAsync(Guid agendamentoId, CancellationToken ct = default);

    Task AddAsync(Fatura fatura, CancellationToken ct = default);

    Task<(IReadOnlyList<Fatura> Items, int TotalCount)> ListAsync(
        Guid? pacienteId, string? status, int page, int pageSize, CancellationToken ct = default);
}
