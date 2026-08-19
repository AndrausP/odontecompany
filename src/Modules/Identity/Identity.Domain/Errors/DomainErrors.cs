using SharedKernel;

namespace Identity.Domain.Errors;

/// <summary>Catálogo de erros de negócio do módulo Identity — sempre tipados, nunca exception pra fluxo esperado.</summary>
public static class DomainErrors
{
    public static class User
    {
        public static readonly Error NomeObrigatorio = new("User.NomeObrigatorio", "Nome é obrigatório.");
        public static readonly Error EmailObrigatorio = new("User.EmailObrigatorio", "Email é obrigatório.");
        public static readonly Error SenhaObrigatoria = new("User.SenhaObrigatoria", "Senha é obrigatória.");
        public static readonly Error EmailJaCadastrado = new("User.EmailJaCadastrado", "Email já cadastrado.");
        public static readonly Error CredenciaisInvalidas = new("User.CredenciaisInvalidas", "Email ou senha inválidos.");
        public static readonly Error UsuarioInativo = new("User.UsuarioInativo", "Usuário inativo.");
        public static readonly Error BranchInvalida = new("User.BranchInvalida", "Branch inválida ou inativa.");
    }

    /// <summary>Erros de <see cref="Entities.OrganizationMembership"/> — task 013 (N:N User↔Organization).</summary>
    public static class Membership
    {
        public static readonly Error OrganizationInvalido = new("Membership.OrganizationInvalido", "Organization inválido.");
        public static readonly Error UserInvalido = new("Membership.UserInvalido", "Usuário inválido.");
        public static readonly Error NaoEncontrada = new("Membership.NaoEncontrada", "Afiliação inexistente ou inativa para esta organization.");
    }

    public static class RefreshTokenErrors
    {
        public static readonly Error Invalido = new("RefreshToken.Invalido", "Refresh token inválido ou expirado.");
    }

    public static class Organization
    {
        public static readonly Error NomeObrigatorio = new("Organization.NomeObrigatorio", "Nome do organization é obrigatório.");
        public static readonly Error NaoEncontrado = new("Organization.NaoEncontrado", "Organization não encontrado.");
        public static readonly Error Inativo = new("Organization.Inativo", "Organization inativo.");
    }

    public static class Seed
    {
        public static readonly Error JaExecutado = new("Seed.JaExecutado", "Seed já foi executado anteriormente — já existe organization cadastrado.");
    }

    /// <summary>Erros de <see cref="Entities.Invite"/> — task 016 (convite de afiliação).</summary>
    public static class Invite
    {
        public static readonly Error OrganizationInvalido = new("Invite.OrganizationInvalido", "Organization inválido.");
        public static readonly Error EmailObrigatorio = new("Invite.EmailObrigatorio", "Email é obrigatório.");
        public static readonly Error TokenInvalido = new("Invite.TokenInvalido", "Token inválido.");

        /// <summary>Mensagem genérica de propósito: nunca distingue "não existe" de "expirado"/"já usado"/"email não bate" — anti-enumeração.</summary>
        public static readonly Error NaoEncontrado = new("Invite.NaoEncontrado", "Convite inválido ou expirado.");
        public static readonly Error EmailJaMembro = new("Invite.EmailJaMembro", "Email já é membro desta organization.");
    }
}
