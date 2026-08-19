using Identity.Domain.Errors;
using SharedKernel;

namespace Identity.Domain.Entities;

/// <summary>Clínica/organização — raiz do isolamento multi-organization (shared schema, organization_id).</summary>
public class Organization : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    public bool Ativo { get; private set; } = true;

    private Organization() { } // EF Core

    private Organization(string nome)
    {
        Nome = nome;
        Ativo = true;
    }

    public static Result<Organization> Create(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Organization>(DomainErrors.Organization.NomeObrigatorio);

        return Result.Success(new Organization(nome.Trim()));
    }

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }
}
