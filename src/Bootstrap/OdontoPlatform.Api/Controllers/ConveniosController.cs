using Billing.Application.Commands.CreateConvenio;
using Billing.Application.Queries.ListConvenios;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Cadastro de convênios credenciados. Criação é Admin-only (configuração da clínica); leitura
/// é Admin+Recepcao (quem cria fatura de convênio via FaturasController precisa listar pra
/// escolher — mesmo racional de ProfissionaisController/SalasController na Fase 5).
/// </summary>
[ApiController]
[Route("api/convenios")]
[Authorize(Roles = "Owner,Admin,Recepcao", Policy = "RequireActiveOrganization")]
public sealed class ConveniosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public ConveniosController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateConvenioRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new CreateConvenioCommand(organizationId.Value, request.Nome, request.CodigoExterno), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListConveniosQuery(includeInactive), ct);
        return Ok(result.Value);
    }
}

public sealed record CreateConvenioRequest(string Nome, string? CodigoExterno);
