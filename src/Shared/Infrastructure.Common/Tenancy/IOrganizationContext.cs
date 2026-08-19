namespace Infrastructure.Common.Tenancy;

/// <summary>
/// Contexto do organization da requisição atual. Instância scoped por request, populada pelo
/// <see cref="OrganizationMiddleware"/> a partir da claim do JWT. Todo DbContext de módulo lê
/// daqui pra aplicar o filtro global de organization.
/// </summary>
public interface IOrganizationContext
{
    Guid? OrganizationId { get; }
    bool IsResolved { get; }
    void SetOrganization(Guid organizationId);
}
