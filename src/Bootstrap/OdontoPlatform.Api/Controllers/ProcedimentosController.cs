using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Commands.CreateProcedimento;
using Scheduling.Application.Commands.DeactivateProcedimento;
using Scheduling.Application.Commands.UpdateProcedimento;
using Scheduling.Application.Queries.ListProcedimentos;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Catálogo de procedimentos/serviços da clínica (task 044 — novo, não fecha lacuna de sprint
/// anterior). Mesmo racional de acesso de `ProfissionaisController`/`SalasController`: criação/
/// edição/desativação é Admin-only, leitura é qualquer autenticado (frontend precisa listar pra
/// montar o select de "Novo agendamento" e o formulário de conclusão).
/// </summary>
[ApiController]
[Route("api/procedimentos")]
[Authorize(Policy = "RequireActiveOrganization")]
public sealed class ProcedimentosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public ProcedimentosController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProcedimentoRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new CreateProcedimentoCommand(organizationId.Value, request.Nome, request.ValorPadrao, request.DuracaoPadraoMinutos), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListProcedimentosQuery(includeInactive), ct);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProcedimentoRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new UpdateProcedimentoCommand(id, organizationId.Value, request.Nome, request.ValorPadrao, request.DuracaoPadraoMinutos), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new DeactivateProcedimentoCommand(id, organizationId.Value), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }
}

public sealed record CreateProcedimentoRequest(string Nome, decimal? ValorPadrao, int? DuracaoPadraoMinutos);

public sealed record UpdateProcedimentoRequest(string Nome, decimal? ValorPadrao, int? DuracaoPadraoMinutos);
