using Contracts.Abstractions.Pagination;
using MediatR;
using Patients.Application.Interfaces;
using Patients.Application.Mapping;
using Patients.Contracts;
using SharedKernel;

namespace Patients.Application.Queries.ListPatients;

public sealed class ListPatientsQueryHandler : IRequestHandler<ListPatientsQuery, Result<PagedResult<PatientDto>>>
{
    private readonly IPatientRepository _patientRepository;

    public ListPatientsQueryHandler(IPatientRepository patientRepository) => _patientRepository = patientRepository;

    public async Task<Result<PagedResult<PatientDto>>> Handle(ListPatientsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _patientRepository.ListAsync(
            request.Nome,
            request.Cpf,
            request.IncludeInactive,
            request.Pagination.Page,
            request.Pagination.PageSize,
            cancellationToken);

        var dtoItems = items.Select(p => p.ToDto()).ToList();

        var pagedResult = PagedResult<PatientDto>.Create(dtoItems, totalCount, request.Pagination.Page, request.Pagination.PageSize);

        return Result.Success(pagedResult);
    }
}
