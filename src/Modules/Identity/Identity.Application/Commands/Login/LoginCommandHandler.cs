using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Identity.Application.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;

    // Hash Argon2id válido, gerado uma única vez por processo (não por request), usado como
    // "senha" quando o usuário não existe — pra Verify() sempre rodar o mesmo custo de CPU e
    // não dar pra enumerar email por diferença de tempo de resposta (short-circuit timing attack).
    private static string? _dummyPasswordHash;
    private static readonly object DummyHashLock = new();

    public LoginCommandHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IOrganizationRepository organizationRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _organizationRepository = organizationRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Verify() SEMPRE roda, mesmo quando o usuário não existe — contra um hash dummy fixo.
        // Se pularmos o Verify pra usuário inexistente (short-circuit), login com email que não
        // existe responde muito mais rápido que email existente + senha errada, e dá pra
        // enumerar emails cadastrados só medindo tempo de resposta.
        var hashToVerify = user?.PasswordHash ?? GetDummyPasswordHash();
        var passwordIsValid = _passwordHasher.Verify(request.Password, hashToVerify);

        // Mensagem genérica de propósito: não revelar se o email existe ou não.
        if (user is null || !passwordIsValid)
            return Result.Failure<LoginResultDto>(DomainErrors.User.CredenciaisInvalidas);

        if (!user.Ativo)
            return Result.Failure<LoginResultDto>(DomainErrors.User.UsuarioInativo);

        var activeMemberships = await _membershipRepository.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, cancellationToken);

        // Usuário sem NENHUMA organization (acabou de fazer signup — task 015 — e ainda não
        // criou/aceitou org nenhuma): emite token válido, mas SEM claim organization_id/role —
        // não null/vazio, ausente mesmo (ver JwtTokenService). Sem refresh token: RefreshToken é
        // IMustHaveOrganization, não existe "refresh token sem org" pra persistir (risco residual
        // documentado na task 014 — usuário nessa situação loga de novo, ou aceita convite antes).
        if (activeMemberships.Count == 0)
        {
            var (soloAccessToken, soloExpiresIn) = _jwtTokenService.GenerateAccessToken(user.Id, organizationId: null, role: null);
            return Result.Success(new LoginResultDto(soloAccessToken, RefreshToken: null, soloExpiresIn));
        }

        // Eleição determinística: a membership mais antiga (critério do Tech Lead — sem UI de
        // seleção nesta sprint, sem estado de "última org usada"). Troca depois via switch-organization.
        var electedMembership = activeMemberships.OrderBy(m => m.CreatedAt).First();

        var organization = await _organizationRepository.GetByIdAsync(electedMembership.OrganizationId, cancellationToken);
        if (organization is null || !organization.Ativo)
            return Result.Failure<LoginResultDto>(DomainErrors.Organization.Inativo);

        var (accessToken, expiresIn) = _jwtTokenService.GenerateAccessToken(
            user.Id, electedMembership.OrganizationId, electedMembership.Role, electedMembership.BranchId);

        var (plainRefreshToken, refreshTokenHash) = _refreshTokenGenerator.Generate();
        var refreshToken = RefreshToken.Create(
            electedMembership.OrganizationId,
            user.Id,
            refreshTokenHash,
            TimeSpan.FromDays(_authOptions.RefreshTokenExpirationDays));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResultDto(accessToken, plainRefreshToken, expiresIn));
    }

    private string GetDummyPasswordHash()
    {
        if (_dummyPasswordHash is not null)
            return _dummyPasswordHash;

        lock (DummyHashLock)
        {
            _dummyPasswordHash ??= _passwordHasher.Hash("dummy-password-only-for-constant-time-login-never-a-real-account");
            return _dummyPasswordHash;
        }
    }
}
