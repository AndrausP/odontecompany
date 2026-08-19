namespace Identity.Domain.Enums;

/// <summary>Papéis RBAC mínimos suportados pela plataforma.</summary>
public enum Role
{
    Admin = 1,
    Dentista = 2,
    Recepcao = 3,

    /// <summary>
    /// Teto do RBAC — atribuído a quem cria a organização (task 013). Aditivo: não substitui
    /// Admin/Dentista/Recepcao, que seguem valendo como estavam. Owner pode convidar e
    /// transferir a organização (fora de escopo desta task — só o papel existe aqui).
    /// </summary>
    Owner = 4
}
