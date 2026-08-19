using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Identity.Application.Commands.CreateOrganization;

/// <summary>
/// Usuário autenticado (com ou sem organization ativa no token) cria uma organization e vira
/// Owner dela automaticamente — Organization + OrganizationMembership + RefreshToken num único
/// <see cref="IUnitOfWork.SaveChangesAsync"/> (atomicidade vem do mesmo DbContext ser a mesma
/// unit-of-work, mesmo padrão de <c>SeedFirstAdminCommandHandler</c>/<c>CreateUserCommandHandler</c>
/// — sem precisar de transação explícita). Devolve um par access+refresh NOVO já escopado à
/// organization recém-criada (decisão do Architect: uma ida a menos pro cliente).
/// </summary>
public sealed class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Result<CreateOrganizationResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;

    public CreateOrganizationCommandHandler(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationMembershipRepository membershipRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
        _membershipRepository = membershipRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<CreateOrganizationResultDto>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.Ativo)
            return Result.Failure<CreateOrganizationResultDto>(DomainErrors.User.UsuarioInativo);

        var organizationResult = Organization.Create(request.Nome, request.Cnpj, request.Telefone, request.Endereco);
        if (organizationResult.IsFailure)
            return Result.Failure<CreateOrganizationResultDto>(organizationResult.Error);

        var organization = organizationResult.Value;

        var membershipResult = OrganizationMembership.Create(organization.Id, user.Id, Role.Owner);
        if (membershipResult.IsFailure)
            return Result.Failure<CreateOrganizationResultDto>(membershipResult.Error);

        await _organizationRepository.AddAsync(organization, cancellationToken);
        await _membershipRepository.AddAsync(membershipResult.Value, cancellationToken);

        var (accessToken, expiresIn) = _jwtTokenService.GenerateAccessToken(user.Id, organization.Id, Role.Owner, branchId: null);

        var (plainRefreshToken, refreshTokenHash) = _refreshTokenGenerator.Generate();
        var refreshToken = RefreshToken.Create(
            organization.Id,
            user.Id,
            refreshTokenHash,
            TimeSpan.FromDays(_authOptions.RefreshTokenExpirationDays));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        // Um único SaveChangesAsync: Organization + Membership + RefreshToken juntos, atômico.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateOrganizationResultDto(organization.Id, organization.Nome, accessToken, plainRefreshToken, expiresIn));
    }
}
