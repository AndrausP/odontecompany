using Contracts.Abstractions.Pagination;
using MediatR;
using Patients.Contracts;
using SharedKernel;

namespace Patients.Application.Queries.ListPatients;

/// <summary>
/// Listagem paginada, escopada por organization via filtro global do EF Core (não recebe OrganizationId —
/// depende só do IOrganizationContext da requisição). Usa <see cref="PageRequest"/> em vez de
/// duplicar a lógica de clamp de Page/PageSize (min 1, max 100) que já existe em
/// Contracts.Abstractions.
/// </summary>
public sealed record ListPatientsQuery(
    PageRequest Pagination,
    string? Nome = null,
    string? Cpf = null,
    bool IncludeInactive = false
) : IRequest<Result<PagedResult<PatientDto>>>;
