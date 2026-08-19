using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Domain.ValueObjects;

/// <summary>
/// Intervalo de tempo de um agendamento — VO imutável, igualdade estrutural. Garante a invariante
/// de negócio "início antes do fim" e a duração mínima (15min) já na criação: nenhum
/// <see cref="Entities.Agendamento"/> consegue existir com um período inválido.
/// </summary>
public sealed class PeriodoHorario : ValueObject
{
    public static readonly TimeSpan DuracaoMinima = TimeSpan.FromMinutes(15);

    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; }

    private PeriodoHorario() { } // EF Core (owned type)

    private PeriodoHorario(DateTime inicio, DateTime fim)
    {
        Inicio = inicio;
        Fim = fim;
    }

    public static Result<PeriodoHorario> Create(DateTime inicio, DateTime fim)
    {
        if (inicio >= fim)
            return Result.Failure<PeriodoHorario>(DomainErrors.PeriodoHorarioErrors.InicioDeveSerAntesDoFim);

        if (fim - inicio < DuracaoMinima)
            return Result.Failure<PeriodoHorario>(DomainErrors.PeriodoHorarioErrors.DuracaoMinimaNaoAtingida);

        return Result.Success(new PeriodoHorario(inicio, fim));
    }

    /// <summary>Sobreposição de intervalos semi-abertos — [Inicio, Fim). Dois períodos que só se tocam na borda (Fim de um == Início do outro) NÃO se sobrepõem.</summary>
    public bool Sobrepoe(PeriodoHorario outro) => Inicio < outro.Fim && outro.Inicio < Fim;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fim;
    }
}
