using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Identity. Implementa <see cref="IOrganizationAwareDbContext"/> pra alimentar
/// o filtro global de organization e <see cref="IUnitOfWork"/> porque o próprio SaveChangesAsync do
/// EF Core já cumpre o contrato — não precisa de classe extra de UoW.
/// </summary>
public sealed class IdentityDbContext : DbContext, IOrganizationAwareDbContext, IUnitOfWork
{
    private readonly IOrganizationContext _organizationContext;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, IOrganizationContext organizationContext) : base(options)
    {
        _organizationContext = organizationContext;
    }

    public Guid? CurrentOrganizationId => _organizationContext.OrganizationId;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Invite> Invites => Set<Invite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Filtro global de organization — aplicado aqui (não dentro de cada IEntityTypeConfiguration)
        // porque a extensão precisa da instância do DbContext (this) pra reavaliar por query.
        // Varre toda entidade do modelo que implementa IMustHaveOrganization (OrganizationMembership,
        // RefreshToken, Invite — Organization fica de fora de propósito, é a própria raiz do
        // organization; User também fica de fora desde a task 013, virou entidade GLOBAL) sem
        // listar uma por uma.
        modelBuilder.ApplyOrganizationQueryFilters(this);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Traduz violação de unique constraint do Postgres (SqlState 23505 — ex: corrida em
    /// EmailExistsAsync seguido de dois inserts concorrentes com o mesmo email) numa
    /// <see cref="UniqueConstraintViolationException"/> que a Application sabe tratar como
    /// Result.Failure, em vez de deixar a exception de infraestrutura estourar como 500 cru.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } pgEx)
        {
            throw new UniqueConstraintViolationException(pgEx.ConstraintName, ex);
        }
    }
}
