namespace Identity.Contracts;

/// <summary>
/// Porta pública do módulo Identity pra outros módulos (e pro host) lerem quem é o usuário
/// autenticado da requisição atual, sem precisar referenciar Identity.Domain/Infrastructure.
/// </summary>
public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? OrganizationId { get; }

    /// <summary>Papel do usuário (Admin/Dentista/Recepcao) como string — cada módulo consumidor
    /// decide se compara direto ou mapeia pro próprio enum local.</summary>
    string? Role { get; }

    /// <summary>
    /// Branch (clínica física) a que o usuário está restrito — Fase 5. Null = admin de rede,
    /// enxerga todas as branches do organization (não presente na claim JWT quando o usuário não tem
    /// BranchId setado). Módulos consumidores (ex: Scheduling) usam isso pra filtrar
    /// listagens quando o papel não é Admin.
    /// </summary>
    Guid? BranchId { get; }
}
