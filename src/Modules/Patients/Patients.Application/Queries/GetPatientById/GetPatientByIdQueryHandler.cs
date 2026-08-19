using MediatR;
using Patients.Application.Interfaces;
using Patients.Application.Mapping;
using Patients.Contracts;
using Patients.Domain.Errors;
using SharedKernel;

namespace Patients.Application.Queries.GetPatientById;

public sealed class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, Result<PatientDto>>
{
    private readonly IPatientRepository _patientRepository;

    public GetPatientByIdQueryHandler(IPatientRepository patientRepository) => _patientRepository = patientRepository;

    public async Task<Result<PatientDto>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);

        return patient is null
            ? Result.Failure<PatientDto>(DomainErrors.Patient.NaoEncontrado)
            : Result.Success(patient.ToDto());
    }
}
