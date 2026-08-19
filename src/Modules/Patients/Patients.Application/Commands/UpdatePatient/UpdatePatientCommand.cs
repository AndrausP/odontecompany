using MediatR;
using Patients.Contracts;
using SharedKernel;

namespace Patients.Application.Commands.UpdatePatient;

/// <summary>
/// Atualiza dados cadastrais. Sem campo Cpf de propósito — CPF é imutável após a criação, não
/// existe caminho pra alterá-lo por este (nem nenhum outro) command.
/// </summary>
public sealed record UpdatePatientCommand(
    Guid Id,
    string NomeCompleto,
    DateTime DataNascimento,
    string Telefone,
    string? Email,
    string? Endereco
) : IRequest<Result<PatientDto>>;
