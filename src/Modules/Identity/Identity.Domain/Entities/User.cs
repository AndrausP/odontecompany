using Identity.Domain.Errors;
using SharedKernel;

namespace Identity.Domain.Entities;

/// <summary>
/// Usuário da plataforma — entidade GLOBAL desde a task 013 (não implementa mais
/// <see cref="IMustHaveOrganization"/>): email é único globalmente, então o usuário em si já não
/// pertence a uma organization específica. Afiliação a organization(s), papel (<see cref="Domain.Enums.Role"/>)
/// e branch são responsabilidade de <see cref="OrganizationMembership"/> — um usuário pode ter 0 a N
/// memberships, em 0 a N organizations, cada uma com seu próprio papel. Sem registro público: só é
/// criado pelo seed (primeiro Admin) ou por um Admin autenticado, sempre acompanhado da
/// <see cref="OrganizationMembership"/> correspondente (nunca um User "solto" com afiliação implícita).
/// </summary>
public class User : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool Ativo { get; private set; } = true;

    private User() { } // EF Core

    private User(string nome, string email, string passwordHash)
    {
        Nome = nome;
        Email = email;
        PasswordHash = passwordHash;
        Ativo = true;
    }

    public static Result<User> Create(string nome, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<User>(DomainErrors.User.NomeObrigatorio);

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>(DomainErrors.User.EmailObrigatorio);

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>(DomainErrors.User.SenhaObrigatoria);

        return Result.Success(new User(nome, email.Trim().ToLowerInvariant(), passwordHash));
    }

    public void AlterarSenha(string novoHash)
    {
        PasswordHash = novoHash;
        SetUpdatedAt();
    }

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }
}
