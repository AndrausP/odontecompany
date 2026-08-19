using Billing.Domain.Errors;
using SharedKernel;

namespace Billing.Domain.Entities;

/// <summary>
/// Convênio de saúde/odontológico credenciado pela clínica. <see cref="CodigoExterno"/> é a
/// referência que o adaptador ACL (<c>IConvenioAdapter</c>, Billing.Application/Infrastructure)
/// usa pra montar a integração externa — o Domain só guarda o código, nunca fala o protocolo do
/// convênio.
/// </summary>
public class Convenio : AggregateRoot, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? CodigoExterno { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Convenio() { } // EF Core

    private Convenio(Guid organizationId, string nome, string? codigoExterno)
    {
        OrganizationId = organizationId;
        Nome = nome;
        CodigoExterno = codigoExterno;
        Ativo = true;
    }

    public static Result<Convenio> Create(Guid organizationId, string nome, string? codigoExterno)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Convenio>(DomainErrors.Fatura.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Convenio>(DomainErrors.Convenio.NomeObrigatorio);

        return Result.Success(new Convenio(organizationId, nome.Trim(), codigoExterno?.Trim()));
    }

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }

    public void Ativar()
    {
        Ativo = true;
        SetUpdatedAt();
    }
}
