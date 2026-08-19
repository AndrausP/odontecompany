namespace SharedKernel;

/// <summary>
/// Base para toda entidade do domínio. Controla identidade (<see cref="Id"/>) e timestamps
/// de auditoria. Igualdade é sempre por identidade, nunca por valor de propriedades.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected Entity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Marca a entidade como alterada agora. Chamar em todo método de negócio que muda estado.</summary>
    protected void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
