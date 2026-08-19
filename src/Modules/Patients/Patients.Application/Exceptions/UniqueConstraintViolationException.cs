namespace Patients.Application.Exceptions;

/// <summary>
/// Exception de aplicação traduzida pela Infrastructure a partir de uma violação de unique
/// constraint no banco (ex: Postgres SqlState 23505 no índice único (OrganizationId, Cpf)). Mesmo
/// padrão do módulo Identity (duplicado de propósito — Application não referencia Application
/// de outro módulo): permite tratar corrida (TOCTOU) como Result.Failure sem a Application
/// referenciar Npgsql/EF diretamente.
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
