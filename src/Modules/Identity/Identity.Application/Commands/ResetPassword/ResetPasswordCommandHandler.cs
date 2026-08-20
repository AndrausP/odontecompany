using Identity.Application.Interfaces;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.ResetPassword;

/// <summary>
/// Troca a senha a partir de um token válido. Revoga TODAS as sessões ativas do usuário (refresh
/// tokens) ao concluir — mesma lógica de qualquer troca de senha por link: se alguém mais tinha
/// uma sessão aberta (ex.: quem esqueceu a senha porque perdeu acesso ao device), essa sessão cai.
/// </summary>
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenGenerator.Hash(request.Token);
        var resetToken = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // Mesma resposta pra "não existe"/"expirado"/"já usado" — anti-enumeração (ver Invite.NaoEncontrado).
        if (resetToken is null || resetToken.IsUsed || resetToken.IsExpired)
            return Result.Failure(DomainErrors.PasswordReset.TokenInvalido);

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null || !user.Ativo)
            return Result.Failure(DomainErrors.PasswordReset.TokenInvalido);

        user.AlterarSenha(_passwordHasher.Hash(request.NovaSenha));
        resetToken.MarkUsed();
        await _refreshTokenRepository.RevokeAllActiveTokensForUserAsync(user.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
