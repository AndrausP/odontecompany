using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetOrganization;

public sealed class GetOrganizationQueryHandler : IRequestHandler<GetOrganizationQuery, Result<OrganizationDto>>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationQueryHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<Result<OrganizationDto>> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null)
            return Result.Failure<OrganizationDto>(DomainErrors.Organization.NaoEncontrado);

        return Result.Success(new OrganizationDto(
            organization.Id, organization.Nome, organization.Cnpj, organization.Telefone, organization.Endereco, organization.Ativo));
    }
}
