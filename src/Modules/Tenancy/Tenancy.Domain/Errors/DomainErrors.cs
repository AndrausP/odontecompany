using SharedKernel;

namespace Tenancy.Domain.Errors;

public static class DomainErrors
{
    public static class Branch
    {
        public static readonly Error OrganizationInvalido = new("Branch.OrganizationInvalido", "Organization inválido.");
        public static readonly Error NomeObrigatorio = new("Branch.NomeObrigatorio", "Nome da branch é obrigatório.");
        public static readonly Error NaoEncontrada = new("Branch.NaoEncontrada", "Branch não encontrada.");
        public static readonly Error Inativa = new("Branch.Inativa", "Branch está inativa.");
        public static readonly Error NomeJaCadastrado = new("Branch.NomeJaCadastrado", "Já existe branch com este nome neste organization.");
    }
}
