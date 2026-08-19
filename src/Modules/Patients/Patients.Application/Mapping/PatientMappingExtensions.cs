using Patients.Contracts;
using Patients.Domain.Entities;

namespace Patients.Application.Mapping;

/// <summary>
/// Mapeamento Domain → Contracts. Mora na Application (não em Patients.Contracts) porque
/// Contracts é a fronteira pública do módulo e não pode depender de Patients.Domain — só quem
/// está dentro do módulo (Application/Infrastructure) enxerga a entidade de domínio.
/// </summary>
public static class PatientMappingExtensions
{
    public static PatientDto ToDto(this Patient patient) => new(
        patient.Id,
        patient.NomeCompleto,
        patient.Cpf.Numero,
        patient.DataNascimento,
        patient.Telefone,
        patient.Email,
        patient.Endereco,
        patient.ConsentimentoLgpd,
        patient.DataConsentimentoLgpd,
        patient.Ativo,
        patient.CreatedAt,
        patient.UpdatedAt);
}
