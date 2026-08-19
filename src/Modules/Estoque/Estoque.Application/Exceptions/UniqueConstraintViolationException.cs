namespace Estoque.Application.Exceptions;

public sealed class UniqueConstraintViolationException : Exception
{
    public string? ConstraintName { get; }

    public UniqueConstraintViolationException(string? constraintName, Exception innerException)
        : base($"Violação de unique constraint: {constraintName}", innerException)
    {
        ConstraintName = constraintName;
    }
}
