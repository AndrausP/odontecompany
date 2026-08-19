namespace Identity.Application.Exceptions;

/// <summary>
/// Exception de aplicação traduzida pela Infrastructure a partir de uma violação de unique
/// constraint no banco (ex: Postgres SqlState 23505). Existe pra Application conseguir tratar
/// corrida (TOCTOU) como falha de negócio esperada (Result.Failure) sem referenciar Npgsql/EF
/// diretamente — quem sabe o detalhe de infraestrutura é a Infrastructure, que traduz aqui.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public string? ConstraintName { get; }

    public UniqueConstraintViolationException(string? constraintName, Exception innerException)
        : base(BuildMessage(constraintName), innerException)
    {
        ConstraintName = constraintName;
    }

    private static string BuildMessage(string? constraintName)
        => constraintName is null
            ? "Violação de unicidade no banco."
            : $"Violação de unicidade no banco (constraint: {constraintName}).";
}
