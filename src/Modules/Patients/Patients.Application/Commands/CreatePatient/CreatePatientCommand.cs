using MediatR;
using Patients.Contracts;
using SharedKernel;

namespace Patients.Application.Commands.CreatePatient;

/// <summary>
/// OrganizationId nunca vem do corpo da requisição HTTP — o controller monta este command com o
/// organization_id extraído da claim do JWT do usuário autenticado (mesmo padrão de
/// CreateUserCommand no módulo Identity), nunca de input do cliente.
/// </summary>
public sealed record CreatePatientCommand(
    Guid OrganizationId,
    string NomeCompleto,
    string Cpf,
    DateTime DataNascimento,
    string Telefone,
    string? Email,
    string? Endereco,
    bool ConsentimentoLgpd
) : IRequest<Result<PatientDto>>;
