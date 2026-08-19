using Estoque.Application.Exceptions;
using Estoque.Application.Interfaces;
using Estoque.Domain.Entities;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Estoque.Infrastructure.Persistence;

public sealed class EstoqueDbContext : DbContext, IOrganizationAwareDbContext, IUnitOfWork
{
    private readonly IOrganizationContext _organizationContext;

    public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options, IOrganizationContext organizationContext) : base(options)
    {
        _organizationContext = organizationContext;
    }

    public Guid? CurrentOrganizationId => _organizationContext.OrganizationId;

    public DbSet<ItemEstoque> ItensEstoque => Set<ItemEstoque>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EstoqueDbContext).Assembly);
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
