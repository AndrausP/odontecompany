using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Contracts;
using Billing.Domain.Entities;
using Billing.Domain.Errors;
using MediatR;
using Patients.Contracts;
using SharedKernel;

namespace Billing.Application.Commands.CreateFaturaParticular;

public sealed class CreateFaturaParticularCommandHandler : IRequestHandler<CreateFaturaParticularCommand, Result<FaturaDto>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IPatientLookup _patientLookup;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFaturaParticularCommandHandler(IFaturaRepository faturaRepository, IPatientLookup patientLookup, IUnitOfWork unitOfWork)
    {
        _faturaRepository = faturaRepository;
        _patientLookup = patientLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FaturaDto>> Handle(CreateFaturaParticularCommand request, CancellationToken cancellationToken)
    {
        if (!await _patientLookup.ExistsAsync(request.PacienteId, cancellationToken))
            return Result.Failure<FaturaDto>(DomainErrors.Fatura.PacienteInvalido);

        var faturaResult = Fatura.CreateParticular(
            request.OrganizationId, request.PacienteId, request.AgendamentoId, request.ProfissionalId,
            request.ValorTotal, request.NumeroParcelas, request.FormaPagamento, request.ComissaoDentistaPercentual);

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
            // Índice único parcial em AgendamentoId (WHERE NOT NULL) — evita faturar a mesma
            // consulta duas vezes em corrida (ex: reprocessamento do consumidor de
            // ConsultaConcluidaEvent). Mesma classe de bug TOCTOU já documentada em
            // docs/knowledge/errors-aprendidos.md, tratada aqui desde o primeiro commit.
            return Result.Failure<FaturaDto>(DomainErrors.Fatura.JaExistePorAgendamento);
        }

        return Result.Success(fatura.ToDto());
    }
}
