using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Domain.Entities;
using Identity.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Identity.Application.Commands.CreateInvite;

/// <summary>
/// Owner/Admin convida um email pra sua organization (task 016). Duas checagens antes de criar:
/// (1) o email já é membro ATIVO da organization → 409 (não cria convite redundante); (2) já
/// existe convite PENDENTE pro mesmo (email, organization) → revoga o anterior e cria um novo
/// (decisão do Architect: workflow simples, "Owner redigita sem pensar em duplicação").
/// </summary>
public sealed class CreateInviteCommandHandler : IRequestHandler<CreateInviteCommand, Result<CreateInviteResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IInviteTokenGenerator _inviteTokenGenerator;
    private readonly IInviteNotifier _inviteNotifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;

    public CreateInviteCommandHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IInviteRepository inviteRepository,
        IInviteTokenGenerator inviteTokenGenerator,
        IInviteNotifier inviteNotifier,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _inviteRepository = inviteRepository;
        _inviteTokenGenerator = inviteTokenGenerator;
        _inviteNotifier = inviteNotifier;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<CreateInviteResultDto>> Handle(CreateInviteCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var invitedUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (invitedUser is not null)
        {
            var existingMembership = await _membershipRepository.GetByUserAndOrganizationAcrossOrganizationsAsync(
                invitedUser.Id, request.OrganizationId, cancellationToken);

            if (existingMembership is not null && existingMembership.IsAtivo)
                return Result.Failure<CreateInviteResultDto>(DomainErrors.Invite.EmailJaMembro);
        }

        var existingInvite = await _inviteRepository.GetPendingByEmailAndOrganizationAsync(email, request.OrganizationId, cancellationToken);
        existingInvite?.Revoke(); // Rastreado pelo EF Core — persistido no mesmo SaveChangesAsync abaixo.

        var (plainToken, tokenHash) = _inviteTokenGenerator.Generate();
        var lifetime = TimeSpan.FromDays(_authOptions.InviteExpirationDays);

        var inviteResult = Invite.Create(request.OrganizationId, email, request.Role, tokenHash, request.InvitedByUserId, lifetime);
        if (inviteResult.IsFailure)
            return Result.Failure<CreateInviteResultDto>(inviteResult.Error);

        var invite = inviteResult.Value;
        await _inviteRepository.AddAsync(invite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // No-op nesta sprint (sem provider de email real) — ver IInviteNotifier/LoggingInviteNotifier.
        await _inviteNotifier.SendInviteAsync(invite.Email, plainToken, invite.OrganizationId, cancellationToken);

        return Result.Success(new CreateInviteResultDto(invite.Id, invite.Email, invite.Role, invite.ExpiresAt, plainToken));
    }
}
