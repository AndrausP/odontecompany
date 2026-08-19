using Scheduling.Application.Interfaces;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Authorization;

/// <summary>
/// Checagem de ownership de agenda — Dentista só pode gerenciar (Confirmar/Cancelar/Concluir)
/// agendamentos da PRÓPRIA agenda; Admin/Recepcao gerenciam qualquer agenda do organization (RBAC de
/// papel, sem restrição de dono). Admin/Recepcao é quem faz a gestão administrativa da clínica —
/// só Dentista tem essa segunda camada de restrição, por ownership de <c>Profissional.UserId</c>.
///
/// Fica em Application (não em cada handler) pra não duplicar a mesma checagem em
/// Confirmar/Cancelar/MarcarAgendamentoConcluido — mesmo racional de qualquer outra regra de
/// negócio compartilhada entre Use Cases do mesmo módulo.
/// </summary>
public static class AgendaOwnershipGuard
{
    private const string DentistaRole = "Dentista";

    /// <summary>
    /// Retorna <see cref="Result.Success"/> se o usuário requisitante pode gerenciar o agendamento
    /// deste <paramref name="profissionalId"/>, ou <see cref="DomainErrors.Agendamento.SemPermissaoAgendaAlheia"/>
    /// se for um Dentista tentando mexer na agenda de outro profissional.
    /// </summary>
    public static async Task<Result> EnsurePodeGerenciarAsync(
        IProfissionalRepository profissionalRepository,
        Guid profissionalId,
        Guid? requestingUserId,
        string? requestingUserRole,
        CancellationToken ct)
    {
        // Admin/Recepcao (ou qualquer papel que não seja Dentista, já filtrado pelo [Authorize]
        // do controller) não tem restrição de ownership — gerenciam qualquer agenda do organization.
        if (!string.Equals(requestingUserRole, DentistaRole, StringComparison.Ordinal))
            return Result.Success();

        var profissional = await profissionalRepository.GetByIdAsync(profissionalId, ct);

        var ehDonoDaAgenda = profissional is not null
            && requestingUserId.HasValue
            && profissional.UserId == requestingUserId.Value;

        return ehDonoDaAgenda ? Result.Success() : Result.Failure(DomainErrors.Agendamento.SemPermissaoAgendaAlheia);
    }
}
