using MediatR;
using Patients.Contracts;
using SharedKernel;

namespace Patients.Application.Queries.GetPatientById;

public sealed record GetPatientByIdQuery(Guid Id) : IRequest<Result<PatientDto>>;
