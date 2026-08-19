namespace Identity.Domain.Enums;

/// <summary>Status do ciclo de vida de um <see cref="Entities.Invite"/> (task 016).</summary>
public enum InviteStatus
{
    Pendente = 1,
    Aceito = 2,
    Expirado = 3,
    Revogado = 4
}
