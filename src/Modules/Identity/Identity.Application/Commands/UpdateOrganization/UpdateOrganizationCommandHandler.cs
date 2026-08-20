using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, Result<OrganizationDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrganizationDto>> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null)
            return Result.Failure<OrganizationDto>(DomainErrors.Organization.NaoEncontrado);

        var updateResult = organization.AtualizarDados(request.Nome, request.Cnpj, request.Telefone, request.Endereco);
        if (updateResult.IsFailure)
            return Result.Failure<OrganizationDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new OrganizationDto(
            organization.Id, organization.Nome, organization.Cnpj, organization.Telefone, organization.Endereco, organization.Ativo));
    }
}
