using Infrastructure.Common.Persistence;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Records.Application.Exceptions;
using Records.Application.Interfaces;
using Records.Domain.Entities;
using Records.Infrastructure.Persistence.Configurations;

namespace Records.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Records — mesmo banco físico dos demais módulos (schema compartilhado,
/// docs/decisions.md), DbContext próprio. Recebe <see cref="IEncryptionService"/> pra construir
/// <see cref="EvolucaoClinicaConfiguration"/> manualmente (ver comentário na própria classe pra
/// entender por que essa configuration não pode vir de <c>ApplyConfigurationsFromAssembly</c>).
/// </summary>
public sealed class RecordsDbContext : DbContext, IOrganizationAwareDbContext, IUnitOfWork
{
    private readonly IOrganizationContext _organizationContext;
    private readonly IEncryptionService _encryptionService;

    public RecordsDbContext(DbContextOptions<RecordsDbContext> options, IOrganizationContext organizationContext, IEncryptionService encryptionService)
        : base(options)
    {
        _organizationContext = organizationContext;
        _encryptionService = encryptionService;
    }

    public Guid? CurrentOrganizationId => _organizationContext.OrganizationId;

    public DbSet<Prontuario> Prontuarios => Set<Prontuario>();
    public DbSet<EvolucaoClinica> EvolucoesClinicas => Set<EvolucaoClinica>();
    public DbSet<AnexoMetadata> AnexosMetadata => Set<AnexoMetadata>();
    public DbSet<RecordsAuditLog> RecordsAuditLog => Set<RecordsAuditLog>();

    // Npgsql só aceita DateTime Kind=Utc pra "timestamp with time zone" — ver nota em
    // PatientsDbContext / Infrastructure.Common.Persistence (achado validando endpoints, sprint-11).
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.ApplyUtcDateTimeConversion();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProntuarioConfiguration());
        modelBuilder.ApplyConfiguration(new EvolucaoClinicaConfiguration(_encryptionService));
        modelBuilder.ApplyConfiguration(new AnexoMetadataConfiguration());
        modelBuilder.ApplyConfiguration(new RecordsAuditLogConfiguration());

        // Filtro global de organization — mesmo método genérico reaproveitado de Infrastructure.Common
        // (task 001). Cobre as 4 entidades IMustHaveOrganization do módulo numa linha só.
        modelBuilder.ApplyOrganizationQueryFilters(this);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Traduz violação de unique constraint do Postgres (ex: dois prontuários pro mesmo paciente) — mesmo padrão dos demais módulos.</summary>
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
