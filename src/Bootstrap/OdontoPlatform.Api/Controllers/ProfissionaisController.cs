using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Commands.CreateProfissional;
using Scheduling.Application.Queries.ListProfissionais;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Cadastro de profissionais (recurso da Agenda). Criação é Admin-only (gestão organizacional,
/// distinta de operar a própria agenda — isso é liberado a Dentista via SchedulingController);
/// leitura é qualquer autenticado (front-end precisa listar pra montar o formulário de
/// agendamento, independente do papel de quem está criando o agendamento).
/// Endpoint que faltava desde a task 004: o módulo Scheduling tinha o agregado `Profissional`
/// mas nenhum jeito de criá-lo/listá-lo pela API — lacuna fechada junto com a Fase 5 (BranchId
/// opcional) e agora com a listagem, necessária pro frontend (Calendário/Agenda).
/// </summary>
[ApiController]
[Route("api/profissionais")]
[Authorize(Policy = "RequireActiveOrganization")]
public sealed class ProfissionaisController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public ProfissionaisController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProfissionalRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreateProfissionalCommand(organizationId.Value, request.Nome, request.Especialidade, request.UserId, request.BranchId);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListProfissionaisQuery(includeInactive), ct);
        return Ok(result.Value);
    }
}

public sealed record CreateProfissionalRequest(string Nome, string Especialidade, Guid? UserId, Guid? BranchId);
