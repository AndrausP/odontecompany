using Contracts.Abstractions;

namespace Patients.Contracts;

/// <summary>
/// Contrato público do módulo Patients — o que outros módulos (ex: Scheduling, task 004) e a
/// API podem consumir. Cpf aqui é string pura (só os 11 dígitos, sem pontuação) de propósito:
/// consumidores externos ao módulo não devem depender do VO de domínio (Patients.Domain.Cpf) —
/// só o próprio módulo Patients enxerga Domain. Mapeamento Patient (entidade) → PatientDto
/// mora em Patients.Application (ver PatientMappingExtensions), nunca aqui.
/// </summary>
public sealed record PatientDto(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    DateTime DataNascimento,
    string Telefone,
    string? Email,
    string? Endereco,
    bool ConsentimentoLgpd,
    DateTime? DataConsentimentoLgpd,
    bool Ativo,
    DateTime CreatedAt,
    DateTime? UpdatedAt
) : IModuleContract;
