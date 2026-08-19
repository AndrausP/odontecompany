using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using SharedKernel;
using DomainRefreshToken = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.Commands.Refresh;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<LoginResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = _refreshTokenGenerator.Hash(request.RefreshToken);

        var existingToken = await _refreshTokenRepository.GetByTokenHashAcrossOrganizationsAsync(incomingHash, cancellationToken);
        if (existingToken is null)
            return Result.Failure<LoginResultDto>(DomainErrors.RefreshTokenErrors.Invalido);

        // Reuso de um token já rotacionado (revogado, mas ainda dentro da validade original) é
        // indício de token roubado: alguém tentou usar um refresh token que já foi trocado por
        // um novo. Corta a sessão inteira do usuário — atacante e vítima precisam logar de novo.
        if (existingToken.IsRevoked && !existingToken.IsExpired)
        {
            await _refreshTokenRepository.RevokeAllActiveTokensForUserAsync(existingToken.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<LoginResultDto>(DomainErrors.RefreshTokenErrors.Invalido);
        }

        if (!existingToken.IsActive)
            return Result.Failure<LoginResultDto>(DomainErrors.RefreshTokenErrors.Invalido);

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.Ativo)
            return Result.Failure<LoginResultDto>(DomainErrors.User.UsuarioInativo);

        // Refresh token É de uma org (a eleita no login/switch — task 013, decisão do Architect).
        // Reelege a membership DESSA org especificamente (não roda eleição nova entre todas as
        // orgs do usuário) — pega o Role/BranchId atuais, cobrindo o caso de terem mudado desde
        // o login. Se o usuário perdeu a afiliação (removido/desativado nessa org), o refresh
        // falha — precisa logar de novo (a sessão daquela org específica não é mais válida).
        var membership = await _membershipRepository.GetByUserAndOrganizationAcrossOrganizationsAsync(
            user.Id, existingToken.OrganizationId, cancellationToken);
        if (membership is null || !membership.IsAtivo)
            return Result.Failure<LoginResultDto>(DomainErrors.RefreshTokenErrors.Invalido);

        var (accessToken, expiresIn) = _jwtTokenService.GenerateAccessToken(user.Id, membership.OrganizationId, membership.Role, membership.BranchId);
        var (plainRefreshToken, newHash) = _refreshTokenGenerator.Generate();

        // Rotação: o token antigo é marcado como usado/revogado, nunca reaproveitado.
        existingToken.Revoke(newHash);

        var newRefreshToken = DomainRefreshToken.Create(
            membership.OrganizationId,
            user.Id,
            newHash,
            TimeSpan.FromDays(_authOptions.RefreshTokenExpirationDays));

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResultDto(accessToken, plainRefreshToken, expiresIn));
    }
}
