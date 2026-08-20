using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Commands.CreateSala;
using Scheduling.Application.Commands.DeactivateSala;
using Scheduling.Application.Commands.UpdateSala;
using Scheduling.Application.Queries.ListSalas;

namespace OdontoPlatform.Api.Controllers;

/// <summary>Cadastro de salas (recurso da Agenda). Criação é Admin-only; leitura é qualquer autenticado (mesmo racional de ProfissionaisController).</summary>
[ApiController]
[Route("api/salas")]
[Authorize(Policy = "RequireActiveOrganization")]
public sealed class SalasController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public SalasController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSalaRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreateSalaCommand(organizationId.Value, request.Nome, request.CapacidadeMaxima, request.BranchId);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListSalasQuery(includeInactive), ct);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalaRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new UpdateSalaCommand(id, organizationId.Value, request.Nome, request.CapacidadeMaxima, request.BranchId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new DeactivateSalaCommand(id, organizationId.Value), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }
}

public sealed record CreateSalaRequest(string Nome, int? CapacidadeMaxima, Guid? BranchId);

public sealed record UpdateSalaRequest(string Nome, int? CapacidadeMaxima, Guid? BranchId);
