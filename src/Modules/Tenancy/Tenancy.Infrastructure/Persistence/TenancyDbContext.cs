using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tenancy.Application.Exceptions;
using Tenancy.Application.Interfaces;
using Tenancy.Domain.Entities;

namespace Tenancy.Infrastructure.Persistence;

public sealed class TenancyDbContext : DbContext, IOrganizationAwareDbContext, IUnitOfWork
{
    private readonly IOrganizationContext _organizationContext;

    public TenancyDbContext(DbContextOptions<TenancyDbContext> options, IOrganizationContext organizationContext) : base(options)
    {
        _organizationContext = organizationContext;
    }

    public Guid? CurrentOrganizationId => _organizationContext.OrganizationId;

    public DbSet<Branch> Branches => Set<Branch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenancyDbContext).Assembly);
        modelBuilder.ApplyOrganizationQueryFilters(this);
        base.OnModelCreating(modelBuilder);
    }

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
