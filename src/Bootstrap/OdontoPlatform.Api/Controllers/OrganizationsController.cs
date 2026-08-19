using Identity.Application.Commands.CreateInvite;
using Identity.Application.Commands.CreateOrganization;
using Identity.Application.Queries.GetOrganizationInvites;
using Identity.Contracts;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Organizações (clínicas). <c>POST</c> aceita explicitamente token SEM organization ativa —
/// é justamente o endpoint que dá a primeira organization a quem não tem nenhuma (task 015),
/// por isso NÃO usa a policy <c>RequireActiveOrganization</c>. Um usuário que já tem organization
/// também pode chamar este endpoint pra criar uma organization adicional (multi-org — vira Owner
/// da nova, sem perder as memberships que já tinha).
/// </summary>
[ApiController]
[Route("api/organizations")]
[Authorize]
public sealed class OrganizationsController : ControllerBase
{
    private const string EmailJaMembroCode = "Invite.EmailJaMembro";

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public OrganizationsController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Cria uma organization; o usuário autenticado vira <c>Owner</c> dela automaticamente.
    /// Retorna <c>201</c> com um par access+refresh NOVO já escopado à organization criada
    /// (uma ida a menos pro cliente — decisão do Architect, task 015).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized();

        var command = new CreateOrganizationCommand(userId.Value, request.Nome);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? Created($"/api/organizations/{result.Value.OrganizationId}", result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Owner/Admin convida um email pra afiliar-se à organization. <paramref name="id"/> tem que
    /// bater com o organization_id do TOKEN do chamador (defesa contra IDOR — nunca confiar cegamente
    /// no id da rota mesmo com a role certa: sem esse check, um Owner de outra organization poderia
    /// escrever convite em organization alheia só trocando o guid da URL).
    /// </summary>
    [HttpPost("{id:guid}/invites")]
    [Authorize(Roles = "Owner,Admin", Policy = "RequireActiveOrganization")]
    public async Task<IActionResult> CreateInvite(Guid id, [FromBody] CreateInviteRequest request, CancellationToken ct)
    {
        if (_currentUser.OrganizationId is null || _currentUser.OrganizationId != id)
            return Forbid();

        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized();

        var command = new CreateInviteCommand(id, userId.Value, request.Email, request.Role);
        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
            return Created($"/api/organizations/{id}/invites/{result.Value.InviteId}", result.Value);

        return result.Error.Code switch
        {
            EmailJaMembroCode => Conflict(new { error = result.Error.Message }),
            _ => BadRequest(new { error = result.Error.Message })
        };
    }

    /// <summary>Lista convites enviados pela organization (Owner/Admin). Mesma defesa contra IDOR do POST acima.</summary>
    [HttpGet("{id:guid}/invites")]
    [Authorize(Roles = "Owner,Admin", Policy = "RequireActiveOrganization")]
    public async Task<IActionResult> GetInvites(Guid id, CancellationToken ct)
    {
        if (_currentUser.OrganizationId is null || _currentUser.OrganizationId != id)
            return Forbid();

        var result = await _mediator.Send(new GetOrganizationInvitesQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }
}

/// <summary>Corpo da requisição de criação de organization.</summary>
public sealed record CreateOrganizationRequest(string Nome);

/// <summary>Corpo da requisição de convite — organization nunca é informado aqui (vem da rota/token).</summary>
public sealed record CreateInviteRequest(string Email, Role Role);
