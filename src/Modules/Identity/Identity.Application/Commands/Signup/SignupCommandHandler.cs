using Identity.Application.DTOs;
using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.Signup;

/// <summary>
/// Signup público: cria <see cref="User"/> GLOBAL, sem organization/membership. Email duplicado
/// devolve o MESMO código de erro genérico usado no resto do módulo (nunca revela se o email já
/// existe — anti-enumeração, mesma lógica do login). Sem organization ainda, o access token sai
/// SEM claim organization_id/role (mesmo padrão do <c>LoginCommandHandler</c> pra usuário com
/// zero memberships) e não há refresh token — ver <see cref="SignupResultDto"/>.
/// </summary>
public sealed class SignupCommandHandler : IRequestHandler<SignupCommand, Result<SignupResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public SignupCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SignupResultDto>> Handle(SignupCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
            return Result.Failure<SignupResultDto>(DomainErrors.User.EmailJaCadastrado);

        var passwordHash = _passwordHasher.Hash(request.Password);

        var userResult = User.Create(request.Nome, email, passwordHash);
        if (userResult.IsFailure)
            return Result.Failure<SignupResultDto>(userResult.Error);

        var user = userResult.Value;
        await _userRepository.AddAsync(user, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // TOCTOU: mesma corrida já documentada em CreateUserCommandHandler — duas requisições
            // concorrentes com o mesmo email passaram pelo EmailExistsAsync antes de qualquer uma
            // comitar. O unique index no banco garante de verdade; aqui só traduzimos pra Result.
            return Result.Failure<SignupResultDto>(DomainErrors.User.EmailJaCadastrado);
        }

        var (accessToken, expiresIn) = _jwtTokenService.GenerateAccessToken(user.Id, organizationId: null, role: null);

        return Result.Success(new SignupResultDto(user.Id, user.Nome, user.Email, accessToken, RefreshToken: null, expiresIn));
    }
}
