using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.AcceptInvite;

/// <summary>
/// Valida token (hash + status Pendente + não expirado) e que o email do convite bate com o
/// email do usuário autenticado, cria a <see cref="OrganizationMembership"/> e marca o convite
/// Aceito — tudo num único <see cref="IUnitOfWork.SaveChangesAsync"/> (atomicidade).
///
/// Toda falha de validação (token inexistente, já usado/revogado, expirado, email não bate)
/// devolve o MESMO <see cref="DomainErrors.Invite.NaoEncontrado"/> — anti-enumeração: nunca
/// revela qual dessas condições especificamente falhou.
/// </summary>
public sealed class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Result<AcceptInviteResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IInviteTokenGenerator _inviteTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptInviteCommandHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IInviteRepository inviteRepository,
        IInviteTokenGenerator inviteTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _inviteRepository = inviteRepository;
        _inviteTokenGenerator = inviteTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AcceptInviteResultDto>> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _inviteTokenGenerator.Hash(request.Token);
        var invite = await _inviteRepository.GetByTokenHashAcrossOrganizationsAsync(tokenHash, cancellationToken);

        if (invite is null || invite.Status != InviteStatus.Pendente)
            return Result.Failure<AcceptInviteResultDto>(DomainErrors.Invite.NaoEncontrado);

        if (invite.IsExpired)
        {
            // Expiração lazy (task 016, item 7): só grava o status Expirado quando alguém de fato
            // tenta usar o convite vencido — sem job de background.
            invite.MarkExpired();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<AcceptInviteResultDto>(DomainErrors.Invite.NaoEncontrado);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.Ativo)
            return Result.Failure<AcceptInviteResultDto>(DomainErrors.User.UsuarioInativo);

        if (!string.Equals(user.Email, invite.Email, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<AcceptInviteResultDto>(DomainErrors.Invite.NaoEncontrado);

        var existingMembership = await _membershipRepository.GetByUserAndOrganizationAcrossOrganizationsAsync(
            user.Id, invite.OrganizationId, cancellationToken);

        if (existingMembership is not null)
        {
            // Idempotência: usuário já é membro (ex: aceitou por engano de novo, ou corrida entre
            // duas abas) — não duplica membership (índice único quebraria), só fecha o convite.
            invite.Accept();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new AcceptInviteResultDto(invite.OrganizationId, existingMembership.Role));
        }

        var membershipResult = OrganizationMembership.Create(invite.OrganizationId, user.Id, invite.Role);
        if (membershipResult.IsFailure)
            return Result.Failure<AcceptInviteResultDto>(membershipResult.Error);

        await _membershipRepository.AddAsync(membershipResult.Value, cancellationToken);
        invite.Accept();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AcceptInviteResultDto(invite.OrganizationId, membershipResult.Value.Role));
    }
}
