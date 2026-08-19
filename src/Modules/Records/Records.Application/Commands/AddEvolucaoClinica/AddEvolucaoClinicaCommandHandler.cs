using MediatR;
using Records.Application.Interfaces;
using Records.Application.Mapping;
using Records.Contracts;
using Records.Domain.Entities;
using Records.Domain.Enums;
using Records.Domain.Errors;
using SharedKernel;

namespace Records.Application.Commands.AddEvolucaoClinica;

public sealed class AddEvolucaoClinicaCommandHandler : IRequestHandler<AddEvolucaoClinicaCommand, Result<EvolucaoClinicaDto>>
{
    private readonly IProntuarioRepository _prontuarioRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IUnitOfWork _unitOfWork;

    public AddEvolucaoClinicaCommandHandler(
        IProntuarioRepository prontuarioRepository, IAuditLogWriter auditLogWriter, IUnitOfWork unitOfWork)
    {
        _prontuarioRepository = prontuarioRepository;
        _auditLogWriter = auditLogWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EvolucaoClinicaDto>> Handle(AddEvolucaoClinicaCommand request, CancellationToken cancellationToken)
    {
        var prontuario = await _prontuarioRepository.GetByIdAsync(request.ProntuarioId, cancellationToken);
        if (prontuario is null)
            return Result.Failure<EvolucaoClinicaDto>(DomainErrors.Prontuario.NaoEncontrado);

        if (!prontuario.Ativo)
            return Result.Failure<EvolucaoClinicaDto>(DomainErrors.Prontuario.Inativo);

        var evolucaoResult = EvolucaoClinica.Create(
            request.OrganizationId, prontuario.Id, request.ProfissionalUserId, request.TipoProcedimento, request.DescricaoClinica);

        if (evolucaoResult.IsFailure)
            return Result.Failure<EvolucaoClinicaDto>(evolucaoResult.Error);

        var evolucao = evolucaoResult.Value;

        await _prontuarioRepository.AddEvolucaoAsync(evolucao, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogWriter.LogAsync(
            request.OrganizationId, prontuario.Id, prontuario.PacienteId, request.ProfissionalUserId,
            AcaoAuditoria.AdicaoEvolucao, cancellationToken);

        return Result.Success(evolucao.ToDto());
    }
}
