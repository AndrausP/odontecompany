using Identity.Application.Commands.AcceptInvite;
using Identity.Application.Queries.GetMyInvites;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Commands.LinkProfissionalUser;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Convites do ponto de vista do CONVIDADO (task 016). Nenhuma ação aqui usa a policy
/// <c>RequireActiveOrganization</c> de propósito: são exatamente os endpoints que precisam
/// funcionar pra um usuário SEM organization ativa ainda (acabou de fazer signup e está aceitando
/// o primeiro convite).
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public sealed class InvitesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public InvitesController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Convites pendentes pro email do usuário logado, em qualquer organization.</summary>
    [HttpGet("me/invites")]
    public async Task<IActionResult> GetMyInvites(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetMyInvitesQuery(userId.Value), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Aceita um convite: cria a membership na organization do convite e marca o convite Aceito.
    /// Não emite token novo — chame <c>POST /api/auth/switch-organization</c> depois se quiser um
    /// token escopado à organization recém-afiliada (ver <c>AcceptInviteResultDto</c>).
    /// Qualquer falha (token inexistente/expirado/já usado/email não bate) responde <c>404</c>
    /// genérico — anti-enumeração.
    ///
    /// Task 042 — depois de aceitar com sucesso, tenta vincular o usuário a qualquer profissional
    /// pendente (Scheduling) com este email — composição no nível do controller, best-effort:
    /// nunca falha o aceite do convite por causa disso (a esmagadora maioria dos convites não é
    /// de dentista com profissional pré-cadastrado esperando).
    /// </summary>
    [HttpPost("invites/{token}/accept")]
    public async Task<IActionResult> Accept(string token, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new AcceptInviteCommand(userId.Value, token), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error.Message });

        await _mediator.Send(new LinkProfissionalUserCommand(result.Value.OrganizationId, result.Value.Email, userId.Value), ct);

        return Ok(result.Value);
    }
}
