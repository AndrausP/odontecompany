namespace Patients.Contracts;

/// <summary>
/// Porta de leitura agregada pro módulo Reporting (task 008) compor dashboards sem violar a
/// fronteira de módulo — mesmo padrão de <see cref="IPatientLookup"/>, mas pra contagem em vez
/// de existência pontual. Implementação (Patients.Infrastructure) só faz `COUNT` sobre a própria
/// tabela, escopado pelo filtro global de organization — nunca JOIN cruzado.
/// </summary>
public interface IPatientSummaryProvider
{
    Task<int> ContarAtivosAsync(Guid organizationId, CancellationToken ct = default);
}
