using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Patients.Application.Exceptions;
using Patients.Application.Interfaces;
using Patients.Domain.Entities;

namespace Patients.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Patients. Mesmo banco físico do Identity (schema compartilhado, ver
/// docs/decisions.md), mas DbContext próprio — sem JOIN cruzado entre módulos, cada um dono do
/// seu conjunto de tabelas. Implementa <see cref="IOrganizationAwareDbContext"/> pro filtro global de
/// organization e <see cref="IUnitOfWork"/> porque o próprio SaveChangesAsync já cumpre o contrato.
/// </summary>
public sealed class PatientsDbContext : DbContext, IOrganizationAwareDbContext, IUnitOfWork
{
    private readonly IOrganizationContext _organizationContext;

    public PatientsDbContext(DbContextOptions<PatientsDbContext> options, IOrganizationContext organizationContext) : base(options)
    {
        _organizationContext = organizationContext;
    }

    public Guid? CurrentOrganizationId => _organizationContext.OrganizationId;

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);

        // Filtro global de organization — varre toda entidade do modelo que implementa
        // IMustHaveOrganization (Patient) sem listar entidade por entidade. Reaproveitado de
        // Infrastructure.Common.Tenancy (task 001), nunca duplicado.
        modelBuilder.ApplyOrganizationQueryFilters(this);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Traduz violação de unique constraint do Postgres (SqlState 23505 — índice único
    /// composto (OrganizationId, Cpf)) numa <see cref="UniqueConstraintViolationException"/> que a
    /// Application sabe tratar como Result.Failure, em vez de deixar a exception de
    /// infraestrutura estourar como 500 cru. Mesmo padrão do IdentityDbContext.
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
