using Identity.Application.Interfaces;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.SkipOnboarding;

/// <summary>
/// Marca que o usuário viu a tela de criar organização e escolheu "por enquanto não" (sprint-11)
/// — persistido pra RequireOrganization (frontend) parar de redirecionar pro /onboarding em
/// qualquer device/sessão futura, até ele de fato criar/entrar numa organization.
/// </summary>
public sealed class SkipOnboardingCommandHandler : IRequestHandler<SkipOnboardingCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SkipOnboardingCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SkipOnboardingCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.Ativo)
            return Result.Failure(DomainErrors.User.UsuarioInativo);

        user.PularOnboarding();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
