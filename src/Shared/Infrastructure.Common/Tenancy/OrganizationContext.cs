namespace Infrastructure.Common.Tenancy;

/// <summary>Implementação scoped padrão de <see cref="IOrganizationContext"/>. Registrar como Scoped no DI.</summary>
public sealed class OrganizationContext : IOrganizationContext
{
    public Guid? OrganizationId { get; private set; }

    public bool IsResolved => OrganizationId.HasValue && OrganizationId.Value != Guid.Empty;

    public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
}
