using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tenancy.Application.Commands.CreateBranch;
using Tenancy.Application.Commands.DeactivateBranch;
using Tenancy.Application.Queries.ListBranches;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Gestão de branches (clínicas) da rede — segundo nível da hierarquia Organization→Branch→Recurso
/// (Fase 5). Admin-only: só o admin de rede gerencia branches, mesmo raciocínio de Convênios.
/// </summary>
[ApiController]
[Route("api/branches")]
[Authorize(Roles = "Owner,Admin", Policy = "RequireActiveOrganization")]
public sealed class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public BranchesController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new CreateBranchCommand(organizationId.Value, request.Nome, request.Endereco, request.Telefone), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeactivateBranchCommand(id), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new ListBranchesQuery(organizationId.Value, includeInactive), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }
}

public sealed record CreateBranchRequest(string Nome, string? Endereco, string? Telefone = null);
