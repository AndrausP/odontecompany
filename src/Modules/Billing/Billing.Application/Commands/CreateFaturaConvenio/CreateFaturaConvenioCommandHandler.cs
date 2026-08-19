using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Contracts;
using Billing.Domain.Entities;
using Billing.Domain.Errors;
using MediatR;
using Patients.Contracts;
using SharedKernel;

namespace Billing.Application.Commands.CreateFaturaConvenio;

/// <summary>
/// Cria fatura de convênio e envia pro convênio externo via <see cref="IConvenioAdapter"/> (ACL).
/// A fatura é persistida ANTES do envio externo — se o envio falhar, a fatura fica registrada
/// (`ProtocoloConvenio` null) e pode ser reenviada depois (reenvio fora de escopo desta task,
/// ver docs/tasks/007-financeiro-convenios.md, riscos residuais). Nunca o inverso: nunca envia
/// pro convênio sem persistir localmente primeiro, senão perde o registro se o processo cair
/// entre o envio e o commit.
/// </summary>
public sealed class CreateFaturaConvenioCommandHandler : IRequestHandler<CreateFaturaConvenioCommand, Result<FaturaDto>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IConvenioRepository _convenioRepository;
    private readonly IPatientLookup _patientLookup;
    private readonly IConvenioAdapter _convenioAdapter;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFaturaConvenioCommandHandler(
        IFaturaRepository faturaRepository,
        IConvenioRepository convenioRepository,
        IPatientLookup patientLookup,
        IConvenioAdapter convenioAdapter,
        IUnitOfWork unitOfWork)
    {
        _faturaRepository = faturaRepository;
        _convenioRepository = convenioRepository;
        _patientLookup = patientLookup;
        _convenioAdapter = convenioAdapter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FaturaDto>> Handle(CreateFaturaConvenioCommand request, CancellationToken cancellationToken)
    {
        if (!await _patientLookup.ExistsAsync(request.PacienteId, cancellationToken))
            return Result.Failure<FaturaDto>(DomainErrors.Fatura.PacienteInvalido);

        var convenio = await _convenioRepository.GetByIdAsync(request.ConvenioId, cancellationToken);
        if (convenio is null)
            return Result.Failure<FaturaDto>(DomainErrors.Fatura.ConvenioInvalido);

        if (!convenio.Ativo)
            return Result.Failure<FaturaDto>(DomainErrors.Convenio.Inativo);

        var faturaResult = Fatura.CreateConvenio(
            request.OrganizationId, request.PacienteId, request.AgendamentoId, request.ProfissionalId,
            request.ConvenioId, request.ValorTotal, request.ComissaoDentistaPercentual);

        if (faturaResult.IsFailure)
            return Result.Failure<FaturaDto>(faturaResult.Error);

        var fatura = faturaResult.Value;

        await _faturaRepository.AddAsync(fatura, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<FaturaDto>(DomainErrors.Fatura.JaExistePorAgendamento);
        }

        // Envio ao convênio acontece DEPOIS de persistir — falha aqui não desfaz a fatura já
        // gravada (é um erro de integração externa, não de negócio; a fatura continua válida
        // sem protocolo, pra reenvio manual/job posterior).
        var envioResult = await _convenioAdapter.EnviarFaturaAsync(fatura, convenio, cancellationToken);
        if (envioResult.IsSuccess)
        {
            fatura.RegistrarProtocoloConvenio(envioResult.Value);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(fatura.ToDto());
    }
}
