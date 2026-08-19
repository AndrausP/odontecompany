using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using SharedKernel;
using DomainRefreshToken = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.Commands.SwitchOrganization;

/// <summary>
/// Troca a organization ativa do usuário autenticado SEM novo login/senha — emite um novo par
/// access+refresh escopado à organization pedida. Task 014, item 3: o token anterior (access +
/// refresh) NÃO é revogado (tokens curtos, 15min — decisão do Architect, complexidade de
/// revogação rejeitada). Membership inexistente ou inativa é sempre <see cref="DomainErrors.Membership.NaoEncontrada"/>
/// (o controller mapeia pra 403 — nunca revela se a organization existe pra quem não é afiliado dela).
/// </summary>
public sealed class SwitchOrganizationCommandHandler : IRequestHandler<SwitchOrganizationCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;

    public SwitchOrganizationCommandHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IOrganizationRepository organizationRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _organizationRepository = organizationRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<LoginResultDto>> Handle(SwitchOrganizationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.Ativo)
            return Result.Failure<LoginResultDto>(DomainErrors.User.UsuarioInativo);

        var membership = await _membershipRepository.GetByUserAndOrganizationAcrossOrganizationsAsync(
            request.UserId, request.OrganizationId, cancellationToken);
        if (membership is null || !membership.IsAtivo)
            return Result.Failure<LoginResultDto>(DomainErrors.Membership.NaoEncontrada);

        var organization = await _organizationRepository.GetByIdAsync(membership.OrganizationId, cancellationToken);
        if (organization is null || !organization.Ativo)
            return Result.Failure<LoginResultDto>(DomainErrors.Organization.Inativo);

        var (accessToken, expiresIn) = _jwtTokenService.GenerateAccessToken(user.Id, membership.OrganizationId, membership.Role, membership.BranchId);

        var (plainRefreshToken, refreshTokenHash) = _refreshTokenGenerator.Generate();
        var refreshToken = DomainRefreshToken.Create(
            membership.OrganizationId,
            user.Id,
            refreshTokenHash,
            TimeSpan.FromDays(_authOptions.RefreshTokenExpirationDays));

        // Token anterior (access + refresh, de qualquer organization) permanece válido até
        // expiração natural — não há revogação aqui de propósito (ver doc da classe).
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResultDto(accessToken, plainRefreshToken, expiresIn));
    }
}
