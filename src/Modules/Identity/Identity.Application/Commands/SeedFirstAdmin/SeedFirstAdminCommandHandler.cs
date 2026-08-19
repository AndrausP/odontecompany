using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.SeedFirstAdmin;

/// <summary>
/// Único jeito de existir o primeiro usuário do sistema — não há endpoint de signup público.
/// Idempotente: se já existe qualquer organization, não faz nada (evita recriar em todo restart
/// de ambiente de desenvolvimento).
/// </summary>
public sealed class SeedFirstAdminCommandHandler : IRequestHandler<SeedFirstAdminCommand, Result<SeedResultDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public SeedFirstAdminCommandHandler(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SeedResultDto>> Handle(SeedFirstAdminCommand request, CancellationToken cancellationToken)
    {
        if (await _organizationRepository.AnyAsync(cancellationToken))
            return Result.Failure<SeedResultDto>(DomainErrors.Seed.JaExecutado);

        var organizationResult = Organization.Create(request.OrganizationNome);
        if (organizationResult.IsFailure)
            return Result.Failure<SeedResultDto>(organizationResult.Error);

        var organization = organizationResult.Value;
        await _organizationRepository.AddAsync(organization, cancellationToken);

        var passwordHash = _passwordHasher.Hash(request.AdminPassword);
        var adminResult = User.Create(request.AdminNome, request.AdminEmail, passwordHash);
        if (adminResult.IsFailure)
            return Result.Failure<SeedResultDto>(adminResult.Error);

        var admin = adminResult.Value;
        await _userRepository.AddAsync(admin, cancellationToken);

        // Primeiro usuário da organization vira Owner (teto do RBAC — task 013), não Admin.
        var membershipResult = OrganizationMembership.Create(organization.Id, admin.Id, Role.Owner);
        if (membershipResult.IsFailure)
            return Result.Failure<SeedResultDto>(membershipResult.Error);

        await _membershipRepository.AddAsync(membershipResult.Value, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SeedResultDto(organization.Id, organization.Nome, admin.Id, admin.Email));
    }
}
