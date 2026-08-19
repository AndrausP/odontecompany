using MediatR;
using Records.Application.Interfaces;
using Records.Application.Mapping;
using Records.Contracts;
using Records.Domain.Enums;
using Records.Domain.Errors;
using SharedKernel;

namespace Records.Application.Queries.GetProntuarioByPacienteId;

/// <summary>
/// Toda leitura de prontuário grava entrada na trilha de auditoria — não é opcional, não tem
/// jeito de ler sem logar. O log acontece DEPOIS da leitura ter sucesso (não loga tentativa de
/// leitura de prontuário inexistente, só acesso real a dado que existe).
/// </summary>
public sealed class GetProntuarioByPacienteIdQueryHandler : IRequestHandler<GetProntuarioByPacienteIdQuery, Result<ProntuarioDto>>
{
    private readonly IProntuarioRepository _prontuarioRepository;
    private readonly IAuditLogWriter _auditLogWriter;

    public GetProntuarioByPacienteIdQueryHandler(IProntuarioRepository prontuarioRepository, IAuditLogWriter auditLogWriter)
    {
        _prontuarioRepository = prontuarioRepository;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<Result<ProntuarioDto>> Handle(GetProntuarioByPacienteIdQuery request, CancellationToken cancellationToken)
    {
        var prontuario = await _prontuarioRepository.GetByPacienteIdAsync(request.PacienteId, cancellationToken);
        if (prontuario is null)
            return Result.Failure<ProntuarioDto>(DomainErrors.Prontuario.NaoEncontrado);

        var evolucoes = await _prontuarioRepository.ListEvolucoesAsync(prontuario.Id, cancellationToken);
        var anexos = await _prontuarioRepository.ListAnexosAsync(prontuario.Id, cancellationToken);

        await _auditLogWriter.LogAsync(
            prontuario.OrganizationId, prontuario.Id, prontuario.PacienteId, request.RequestingUserId,
            AcaoAuditoria.Leitura, cancellationToken);

        return Result.Success(prontuario.ToDto(evolucoes, anexos));
    }
}
