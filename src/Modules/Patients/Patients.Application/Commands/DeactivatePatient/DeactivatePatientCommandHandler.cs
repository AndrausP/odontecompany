using MediatR;
using Patients.Application.Interfaces;
using Patients.Domain.Errors;
using SharedKernel;

namespace Patients.Application.Commands.DeactivatePatient;

public sealed class DeactivatePatientCommandHandler : IRequestHandler<DeactivatePatientCommand, Result>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivatePatientCommandHandler(IPatientRepository patientRepository, IUnitOfWork unitOfWork)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (patient is null)
            return Result.Failure(DomainErrors.Patient.NaoEncontrado);

        patient.Desativar(); // soft delete — Ativo = false, a linha continua no banco

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
