using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;
using Subscriptions.Contracts;

namespace Identity.Application.Commands.AcceptInvite;

/// <summary>
/// Valida token (hash + status Pendente + não expirado) e que o email do convite bate com o
/// email do usuário autenticado, cria a <see cref="OrganizationMembership"/> e marca o convite
/// Aceito — tudo num único <see cref="IUnitOfWork.SaveChangesAsync"/> (atomicidade).
///
/// Toda falha de validação (token inexistente, já usado/revogado, expirado, email não bate)
/// devolve o MESMO <see cref="DomainErrors.Invite.NaoEncontrado"/> — anti-enumeração: nunca
/// revela qual dessas condições especificamente falhou.
///
/// Limite de usuário por plano (auditoria pré-venda) — mesmo raciocínio do limite de filial em
/// <c>CreateBranchCommandHandler</c>: checado aqui, no momento exato em que uma membership ATIVA
/// nova entraria pra organização (convite só cria/reativa a membership no ACCEPT, nunca no
/// envio — checar no CreateInvite não pegaria o caso real de N convites pendentes todos aceitos
/// depois).
/// </summary>
public sealed class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Result<AcceptInviteResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IInviteTokenGenerator _inviteTokenGenerator;
    private readonly ISubscriptionLookup _subscriptionLookup;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptInviteCommandHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IInviteRepository inviteRepository,
        IInviteTokenGenerator inviteTokenGenerator,
        ISubscriptionLookup subscriptionLookup,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _inviteRepository = inviteRepository;
        _inviteTokenGenerator = inviteTokenGenerator;
        _subscriptionLookup = subscriptionLookup;
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

        if (existingMembership is not null && existingMembership.IsAtivo)
        {
            // Idempotência: usuário já é membro ativo (ex: aceitou por engano de novo, ou corrida
            // entre duas abas) — não duplica membership (índice único quebraria), só fecha o
            // convite, sem mudar o papel já existente. Não conta contra o limite (não é membro
            // NOVO, já era ativo).
            invite.Accept();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new AcceptInviteResultDto(invite.OrganizationId, existingMembership.Role, invite.Email));
        }

        // Daqui pra baixo, os dois caminhos restantes (reativar OU criar) resultam numa membership
        // ATIVA NOVA — é aqui que o limite de usuário do plano se aplica.
        var limiteUsuarios = await _subscriptionLookup.LimiteDeUsuariosAsync(invite.OrganizationId, cancellationToken);
        var usuariosAtivos = await _membershipRepository.CountActiveByOrganizationAcrossOrganizationsAsync(invite.OrganizationId, cancellationToken);
        if (usuariosAtivos >= limiteUsuarios)
            return Result.Failure<AcceptInviteResultDto>(DomainErrors.Membership.LimiteDoPlanoAtingido);

        if (existingMembership is not null)
        {
            // Task 020 — membership tinha sido desativada (ex: Owner removeu, depois convidou
            // de novo). Reativa em vez de ficar presa em Inativo com resposta de sucesso
            // (bug latente descrito em docs/knowledge/errors-aprendidos.md). Papel do convite
            // novo prevalece — ver nota em OrganizationMembership.Reativar.
            existingMembership.Reativar(invite.Role);
            invite.Accept();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new AcceptInviteResultDto(invite.OrganizationId, existingMembership.Role, invite.Email));
        }

        var membershipResult = OrganizationMembership.Create(invite.OrganizationId, user.Id, invite.Role);
        if (membershipResult.IsFailure)
            return Result.Failure<AcceptInviteResultDto>(membershipResult.Error);

        await _membershipRepository.AddAsync(membershipResult.Value, cancellationToken);
        invite.Accept();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AcceptInviteResultDto(invite.OrganizationId, membershipResult.Value.Role, invite.Email));
    }
}
