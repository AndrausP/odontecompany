namespace Records.Domain.Enums;

/// <summary>Estado clínico de um dente no odontograma. Numeração FDI (11-18, 21-28, 31-38, 41-48).</summary>
public enum StatusDente
{
    Higido,
    Cariado,
    Restaurado,
    Extraido,
    Ausente,
    ImplanteProtese,
    TratamentoCanal
}
