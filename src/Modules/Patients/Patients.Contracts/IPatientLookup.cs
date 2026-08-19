namespace Patients.Contracts;

/// <summary>
/// Porta pública do módulo Patients pra outros módulos (ex: Scheduling, task 004) checarem
/// existência de paciente sem depender de Patients.Domain/Patients.Infrastructure — mesmo padrão
/// de <c>Identity.Contracts.ICurrentUserAccessor</c>. Implementação mora em Patients.Infrastructure
/// e é registrada no DI do host; quem consome (ex: Scheduling.Application) só referencia este
/// projeto (Patients.Contracts), nunca os outros dois.
/// </summary>
public interface IPatientLookup
{
    /// <summary>
    /// Existe paciente ATIVO com este Id? Escopado pelo filtro global de organization do próprio módulo
    /// Patients (via IOrganizationContext da requisição) — não recebe OrganizationId explícito de propósito,
    /// pra não abrir brecha de checar paciente de organization arbitrário por fora do contexto da requisição.
    /// </summary>
    Task<bool> ExistsAsync(Guid patientId, CancellationToken ct = default);
}
