namespace Infrastructure.Common.Tenancy;

/// <summary>
/// Todo DbContext de módulo que aplica filtro global de organization implementa isto — expõe o
/// organization atual como membro do próprio contexto, porque é assim que o EF Core consegue
/// reavaliar o filtro a cada query (o model é compilado uma vez; o valor lido do membro do
/// DbContext, não).
/// </summary>
public interface IOrganizationAwareDbContext
{
    Guid? CurrentOrganizationId { get; }
}
