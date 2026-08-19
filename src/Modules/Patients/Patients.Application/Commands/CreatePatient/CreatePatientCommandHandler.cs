using MediatR;
using Patients.Application.Exceptions;
using Patients.Application.Interfaces;
using Patients.Application.Mapping;
using Patients.Contracts;
using Patients.Domain.Entities;
using Patients.Domain.Errors;
using SharedKernel;

namespace Patients.Application.Commands.CreatePatient;

public sealed class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePatientCommandHandler(IPatientRepository patientRepository, IUnitOfWork unitOfWork)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PatientDto>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        if (await _patientRepository.CpfExistsAsync(request.OrganizationId, request.Cpf, cancellationToken))
            return Result.Failure<PatientDto>(DomainErrors.Patient.CpfJaCadastrado);

        var patientResult = Patient.Create(
            request.OrganizationId,
            request.NomeCompleto,
            request.Cpf,
            request.DataNascimento,
            request.Telefone,
            request.Email,
            request.Endereco,
            request.ConsentimentoLgpd);

        if (patientResult.IsFailure)
            return Result.Failure<PatientDto>(patientResult.Error);

        var patient = patientResult.Value;

        await _patientRepository.AddAsync(patient, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // TOCTOU: duas requisições concorrentes com o mesmo CPF no mesmo organization passaram
            // pelo CpfExistsAsync antes de qualquer uma commitar. O índice único (OrganizationId, Cpf)
            // é quem garante a integridade de verdade — aqui só traduzimos pra Result de negócio.
            return Result.Failure<PatientDto>(DomainErrors.Patient.CpfJaCadastrado);
        }

        return Result.Success(patient.ToDto());
    }
}
