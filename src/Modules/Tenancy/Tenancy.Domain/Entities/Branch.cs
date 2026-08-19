using Tenancy.Domain.Errors;
using SharedKernel;

namespace Tenancy.Domain.Entities;

/// <summary>
/// Branch (clínica física) dentro de um Organization (rede/empresa) — segundo nível da hierarquia
/// `Organization → Branch → Recurso` do doc de arquitetura. Extensão ADITIVA sobre o modelo
/// multi-organization existente: `OrganizationId` continua sendo o escopo primário de isolamento (shared
/// schema, filtro global — nada muda pros módulos já prontos), `BranchId` é um nível OPCIONAL
/// abaixo dele, referenciado por recursos físicos (Profissional/Sala do Scheduling) e por
/// usuários não-admin (User.BranchId do Identity) que devem enxergar só a própria branch.
/// Admin de rede (sem BranchId setado) continua vendo TODAS as branches do organization.
/// </summary>
public class Branch : AggregateRoot, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Endereco { get; private set; }
    // Opcional (sprint-11) — "configurar depois", mesma lógica de Organization.Cnpj/Telefone.
    public string? Telefone { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Branch() { } // EF Core

    private Branch(Guid organizationId, string nome, string? endereco, string? telefone)
    {
        OrganizationId = organizationId;
        Nome = nome;
        Endereco = endereco;
        Telefone = telefone;
        Ativo = true;
    }

    public static Result<Branch> Create(Guid organizationId, string nome, string? endereco, string? telefone = null)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Branch>(DomainErrors.Branch.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Branch>(DomainErrors.Branch.NomeObrigatorio);

        return Result.Success(new Branch(
            organizationId,
            nome.Trim(),
            string.IsNullOrWhiteSpace(endereco) ? null : endereco.Trim(),
            string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim()));
    }

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }
}
