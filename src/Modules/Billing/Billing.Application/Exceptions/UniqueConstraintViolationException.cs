namespace Billing.Application.Exceptions;

/// <summary>Traduzida a partir de violação de unique constraint do Postgres (SqlState 23505) — mesmo padrão dos demais módulos.</summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public string? ConstraintName { get; }

    public UniqueConstraintViolationException(string? constraintName, Exception innerException)
        : base($"Violação de unique constraint: {constraintName}", innerException)
    {
        ConstraintName = constraintName;
    }
}
