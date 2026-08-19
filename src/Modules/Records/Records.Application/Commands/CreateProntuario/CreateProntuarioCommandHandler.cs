using MediatR;
using Patients.Contracts;
using Records.Application.Exceptions;
using Records.Application.Interfaces;
using Records.Application.Mapping;
using Records.Contracts;
using Records.Domain.Entities;
using Records.Domain.Enums;
using Records.Domain.Errors;
using SharedKernel;

namespace Records.Application.Commands.CreateProntuario;

/// <summary>
/// Cria o prontuário de um paciente. Referencia <c>Patients.Contracts.IPatientLookup</c> (nunca
/// Patients.Domain/Infrastructure) pra confirmar que o paciente existe e está ativo antes de
/// abrir prontuário — mesmo padrão de fronteira cross-module já usado por Scheduling (task 004).
/// </summary>
public sealed class CreateProntuarioCommandHandler : IRequestHandler<CreateProntuarioCommand, Result<ProntuarioDto>>
{
    private readonly IProntuarioRepository _prontuarioRepository;
    private readonly IPatientLookup _patientLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProntuarioCommandHandler(
        IProntuarioRepository prontuarioRepository,
        IPatientLookup patientLookup,
        IAuditLogWriter auditLogWriter,
        IUnitOfWork unitOfWork)
    {
        _prontuarioRepository = prontuarioRepository;
        _patientLookup = patientLookup;
        _auditLogWriter = auditLogWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProntuarioDto>> Handle(CreateProntuarioCommand request, CancellationToken cancellationToken)
    {
        if (!await _patientLookup.ExistsAsync(request.PacienteId, cancellationToken))
            return Result.Failure<ProntuarioDto>(DomainErrors.Prontuario.PacienteNaoEncontrado);

        if (await _prontuarioRepository.ExistsByPacienteIdAsync(request.PacienteId, cancellationToken))
            return Result.Failure<ProntuarioDto>(DomainErrors.Prontuario.JaExistePorPaciente);

        var prontuarioResult = Prontuario.Create(request.OrganizationId, request.PacienteId, request.CriadoPorUserId);
        if (prontuarioResult.IsFailure)
            return Result.Failure<ProntuarioDto>(prontuarioResult.Error);

        var prontuario = prontuarioResult.Value;

        await _prontuarioRepository.AddAsync(prontuario, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // TOCTOU: duas requisições concorrentes pro mesmo paciente passaram por
            // ExistsByPacienteIdAsync antes de qualquer uma commitar. O índice único
            // (OrganizationId, PacienteId) é quem garante a integridade de verdade — mesmo padrão já
            // usado em Identity (email) e Patients (CPF), ver docs/knowledge/errors-aprendidos.md.
            return Result.Failure<ProntuarioDto>(DomainErrors.Prontuario.JaExistePorPaciente);
        }

        await _auditLogWriter.LogAsync(
            request.OrganizationId, prontuario.Id, prontuario.PacienteId, request.CriadoPorUserId, AcaoAuditoria.Criacao, cancellationToken);

        return Result.Success(prontuario.ToDto([], []));
    }
}
