using Identity.Domain.Errors;
using SharedKernel;

namespace Identity.Domain.Entities;

/// <summary>Clínica/organização — raiz do isolamento multi-organization (shared schema, organization_id).</summary>
public class Organization : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    // Cnpj/Telefone/Endereco opcionais de propósito (sprint-11) — "configurar depois": onboarding
    // não trava quem só quer criar rápido, dado real entra quando o Owner quiser (tela de
    // configurações da organização, fora de escopo desta rodada — hoje só entram na criação).
    public string? Cnpj { get; private set; }
    public string? Telefone { get; private set; }
    public string? Endereco { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Organization() { } // EF Core

    private Organization(string nome, string? cnpj, string? telefone, string? endereco)
    {
        Nome = nome;
        Cnpj = cnpj;
        Telefone = telefone;
        Endereco = endereco;
        Ativo = true;
    }

    public static Result<Organization> Create(string nome, string? cnpj = null, string? telefone = null, string? endereco = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Organization>(DomainErrors.Organization.NomeObrigatorio);

        return Result.Success(new Organization(
            nome.Trim(),
            string.IsNullOrWhiteSpace(cnpj) ? null : cnpj.Trim(),
            string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
            string.IsNullOrWhiteSpace(endereco) ? null : endereco.Trim()));
    }

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }
}
