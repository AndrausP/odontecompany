namespace SharedKernel;

/// <summary>Contrato de evento de domínio, disparado por um <see cref="AggregateRoot"/>.</summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
