using MediatR;
using SharedKernel;

namespace Patients.Application.Commands.DeactivatePatient;

/// <summary>Soft delete — marca o paciente como inativo. Nunca remove a linha do banco.</summary>
public sealed record DeactivatePatientCommand(Guid Id) : IRequest<Result>;
