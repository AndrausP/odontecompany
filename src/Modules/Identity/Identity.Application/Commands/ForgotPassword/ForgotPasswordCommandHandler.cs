using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Gera um token de redefinição se o email existir e o usuário estiver ativo — silenciosamente
/// não faz nada caso contrário (nunca retorna Failure por "email não encontrado", pra não vazar
/// quem tem conta). Mesmo padrão de token/notifier de <c>CreateInviteCommandHandler</c>.
/// </summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IPasswordResetTokenGenerator _tokenGenerator;
    private readonly IPasswordResetNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IPasswordResetTokenGenerator tokenGenerator,
        IPasswordResetNotifier notifier,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _tokenGenerator = tokenGenerator;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.Ativo)
            return Result.Success(); // anti-enumeração — mesmo resultado de um pedido válido

        await _tokenRepository.InvalidateActiveTokensForUserAsync(user.Id, cancellationToken);

        var (plainToken, tokenHash) = _tokenGenerator.Generate();
        var lifetime = TimeSpan.FromHours(_authOptions.PasswordResetExpirationHours);

        var tokenResult = PasswordResetToken.Create(user.Id, tokenHash, lifetime);
        if (tokenResult.IsFailure)
            return Result.Success(); // mesma lógica: não vaza erro interno pro anônimo

        await _tokenRepository.AddAsync(tokenResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // No-op nesta rodada (sem provider de email real) — ver IPasswordResetNotifier/LoggingPasswordResetNotifier.
        await _notifier.SendResetLinkAsync(user.Email, plainToken, cancellationToken);

        return Result.Success();
    }
}
