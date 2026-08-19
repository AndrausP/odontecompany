using MediatR;
using Patients.Application.Interfaces;
using Patients.Application.Mapping;
using Patients.Contracts;
using Patients.Domain.Errors;
using SharedKernel;

namespace Patients.Application.Commands.UpdatePatient;

public sealed class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result<PatientDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePatientCommandHandler(IPatientRepository patientRepository, IUnitOfWork unitOfWork)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PatientDto>> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        // GetByIdAsync já é escopado pelo filtro global de organization — um Id de paciente de outro
        // organization simplesmente não é encontrado, sem precisar de checagem de organization explícita aqui.
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (patient is null)
            return Result.Failure<PatientDto>(DomainErrors.Patient.NaoEncontrado);

        var updateResult = patient.AtualizarDadosCadastrais(
            request.NomeCompleto,
            request.DataNascimento,
            request.Telefone,
            request.Email,
            request.Endereco);

        if (updateResult.IsFailure)
            return Result.Failure<PatientDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(patient.ToDto());
    }
}
