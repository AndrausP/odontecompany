namespace Scheduling.Application.Exceptions;

/// <summary>
/// Exception de aplicação traduzida pela Infrastructure a partir de um <c>DbUpdateConcurrencyException</c>
/// do EF Core (conflito no token otimista <c>xmin</c>). Mesmo racional de
/// <c>UniqueConstraintViolationException</c> (Identity/Patients): permite tratar o conflito como
/// <c>Result.Failure</c> tipado sem a Application referenciar <c>Microsoft.EntityFrameworkCore</c>
/// diretamente — Application não tem (e não deve ter) PackageReference de EF Core.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception innerException)
        : base("Conflito de concorrência otimista ao salvar o agendamento.", innerException)
    {
    }
}
