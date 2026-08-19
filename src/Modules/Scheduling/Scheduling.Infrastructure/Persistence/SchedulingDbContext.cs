using System.Text.Json;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Exceptions;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using SharedKernel;

namespace Scheduling.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Scheduling. Implementa <see cref="IOrganizationAwareDbContext"/> pro filtro
/// global de organization e <see cref="IUnitOfWork"/> porque o próprio SaveChangesAsync já cumpre o
/// contrato — mesmo padrão de IdentityDbContext/PatientsDbContext.
/// </summary>
public sealed class SchedulingDbContext : DbContext, IOrganizationAwareDbContext, IUnitOfWork
{
    private readonly IOrganizationContext _organizationContext;

    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options, IOrganizationContext organizationContext) : base(options)
    {
        _organizationContext = organizationContext;
    }

    public Guid? CurrentOrganizationId => _organizationContext.OrganizationId;

    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<SchedulingOutboxMessage> OutboxMessages => Set<SchedulingOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);

        // Filtro global de organization — varre toda entidade do modelo que implementa IMustHaveOrganization
        // (Agendamento, Profissional, Sala) sem listar entidade por entidade. SchedulingOutboxMessage
        // fica de fora de propósito: não implementa IMustHaveOrganization, o payload já carrega o
        // OrganizationId serializado dentro do JSON do evento.
        modelBuilder.ApplyOrganizationQueryFilters(this);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Antes de persistir: converte todo <c>Scheduling.Domain.Events.ConsultaConcluidaEvent</c>
    /// pendente nos agregados rastreados numa linha de Outbox, gravada NA MESMA transação deste
    /// SaveChangesAsync — se o commit falhar, a entry de outbox também não existe (atomicidade).
    /// Depois traduz <see cref="DbUpdateConcurrencyException"/> (conflito no token otimista xmin)
    /// pra <see cref="ConcurrencyConflictException"/>, que a Application sabe tratar como
    /// Result.Failure sem referenciar EF Core diretamente.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnqueueDomainEventsAsOutboxMessages();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }

    private void EnqueueDomainEventsAsOutboxMessages()
    {
        var aggregatesComEventos = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregatesComEventos)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                if (domainEvent is Scheduling.Domain.Events.ConsultaConcluidaEvent concluida)
                {
                    var contractEvent = new Scheduling.Contracts.ConsultaConcluidaEvent(
                        concluida.AgendamentoId,
                        concluida.OrganizationId,
                        concluida.PacienteId,
                        concluida.ProfissionalId,
                        concluida.DataHoraConclusao,
                        concluida.Valor);

                    OutboxMessages.Add(SchedulingOutboxMessage.Create(
                        nameof(Scheduling.Contracts.ConsultaConcluidaEvent),
                        JsonSerializer.Serialize(contractEvent)));
                }
            }

            aggregate.ClearDomainEvents();
        }
    }
}
