using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel;

namespace Infrastructure.Common.Tenancy;

public static class OrganizationQueryFilterExtensions
{
    private static readonly MethodInfo HasOrganizationQueryFilterMethod = typeof(OrganizationQueryFilterExtensions)
        .GetMethod(nameof(HasOrganizationQueryFilter), BindingFlags.Public | BindingFlags.Static)!;

    // ModelBuilder.Entity<TEntity>() sem argumentos — a sobrecarga genérica que devolve
    // EntityTypeBuilder<TEntity> (o tipo que HasOrganizationQueryFilter<TEntity,TContext> espera).
    // `Entity(Type)` (não-genérica) devolve só EntityTypeBuilder base, incompatível.
    private static readonly MethodInfo ModelBuilderEntityMethod = typeof(ModelBuilder)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(m => m.Name == nameof(ModelBuilder.Entity)
                     && m.IsGenericMethodDefinition
                     && m.GetGenericArguments().Length == 1
                     && m.GetParameters().Length == 0);

    /// <summary>
    /// Aplica o filtro global <c>organization_id = CurrentOrganizationId</c> numa entidade específica que
    /// implemente <see cref="IMustHaveOrganization"/>. Use direto quando precisar configurar o
    /// filtro entidade a entidade (ex: junto de outras chamadas em cima do mesmo
    /// <see cref="EntityTypeBuilder{TEntity}"/>); pra aplicar em todo o modelo de uma vez,
    /// veja <see cref="ApplyOrganizationQueryFilters{TContext}"/>.
    /// </summary>
    public static void HasOrganizationQueryFilter<TEntity, TContext>(this EntityTypeBuilder<TEntity> builder, TContext context)
        where TEntity : class, IMustHaveOrganization
        where TContext : DbContext, IOrganizationAwareDbContext
    {
        builder.HasQueryFilter(e => e.OrganizationId == context.CurrentOrganizationId);
    }

    /// <summary>
    /// Aplica o filtro global de organization em TODA entidade do modelo que implemente
    /// <see cref="IMustHaveOrganization"/> — sem precisar listar entidade por entidade em cada
    /// DbContext de módulo. Chamar em <c>OnModelCreating</c>, depois de
    /// <c>ApplyConfigurationsFromAssembly</c>, passando o próprio contexto (<c>this</c>):
    /// <code>modelBuilder.ApplyOrganizationQueryFilters(this);</code>
    ///
    /// Importante: recebe o <typeparamref name="TContext"/> (a instância do próprio
    /// DbContext), não um <see cref="IOrganizationContext"/> solto. O EF Core compila o model uma
    /// única vez e cacheia entre requisições — se o filtro capturasse um serviço scoped
    /// externo diretamente, ficaria congelado pra sempre com o organization da primeira requisição
    /// que construiu o model (vazamento entre organizations). Referenciar um membro do próprio
    /// DbContext (<see cref="IOrganizationAwareDbContext.CurrentOrganizationId"/>) é o que faz o EF Core
    /// reavaliar o filtro usando a instância viva a cada query.
    /// </summary>
    public static void ApplyOrganizationQueryFilters<TContext>(this ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, IOrganizationAwareDbContext
    {
        // Snapshot da lista antes do loop: GetEntityTypes() é lido do model builder, e
        // HasQueryFilter mais adiante mexe no mesmo model — iterar direto do enumerable
        // vivo arriscaria "collection modified" dependendo da versão do EF Core.
        var organizationEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(IMustHaveOrganization).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType)
            .ToList();

        foreach (var clrType in organizationEntityTypes)
        {
            var entityBuilder = ModelBuilderEntityMethod.MakeGenericMethod(clrType).Invoke(modelBuilder, null);
            var genericMethod = HasOrganizationQueryFilterMethod.MakeGenericMethod(clrType, typeof(TContext));
            genericMethod.Invoke(null, [entityBuilder, context]);
        }
    }
}
