namespace SharedKernel;

/// <summary>
/// Raiz de agregado — única entidade dentro de um agregado que pode ser referenciada de fora
/// e a única que acumula <see cref="IDomainEvent"/> pra ser despachado depois do commit.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() : base() { }

    protected AggregateRoot(Guid id) : base(id) { }

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
