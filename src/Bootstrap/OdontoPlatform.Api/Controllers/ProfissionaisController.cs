using Identity.Application.Commands.CreateInvite;
using Identity.Contracts;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Commands.CreateProfissional;
using Scheduling.Application.Queries.ListProfissionais;
using Scheduling.Contracts;
using Scheduling.Domain.Enums;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Cadastro de profissionais (recurso da Agenda). Criação é Admin-only (gestão organizacional,
/// distinta de operar a própria agenda — isso é liberado a Dentista via SchedulingController);
/// leitura é qualquer autenticado (front-end precisa listar pra montar o formulário de
/// agendamento, independente do papel de quem está criando o agendamento).
/// Endpoint que faltava desde a task 004: o módulo Scheduling tinha o agregado `Profissional`
/// mas nenhum jeito de criá-lo/listá-lo pela API — lacuna fechada junto com a Fase 5 (BranchId
/// opcional) e agora com a listagem, necessária pro frontend (Calendário/Agenda).
///
/// Task 042 — `Email` opcional na criação: se informado, o controller ORQUESTRA a criação do
/// profissional (Scheduling) + um convite Role.Dentista (Identity) na mesma requisição. Isso é
/// composição de módulos no nível certo (Bootstrap), não um módulo chamando o outro por dentro —
/// nenhum dos dois Application layers passa a depender do outro por causa disso. Convite é
/// best-effort: se falhar (ex.: email já é membro), o profissional continua criado — o aviso vai
/// no corpo da resposta, não vira erro 400 pra quem só queria cadastrar o recurso.
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

        var command = new CreateProfissionalCommand(
            organizationId.Value, request.Nome, request.Especialidade, request.TipoContrato,
            request.PercentualComissaoDefault, request.Email, request.UserId, request.BranchId);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error.Message });

        if (string.IsNullOrWhiteSpace(request.Email))
            return Ok(new CreateProfissionalResponse(result.Value, ConviteEnviado: false, ConviteAviso: null));

        var userId = _currentUser.UserId;
        if (userId is null)
            return Ok(new CreateProfissionalResponse(result.Value, ConviteEnviado: false, ConviteAviso: "Não foi possível identificar quem convidou."));

        var inviteCommand = new CreateInviteCommand(organizationId.Value, userId.Value, request.Email, Role.Dentista);
        var inviteResult = await _mediator.Send(inviteCommand, ct);

        return Ok(inviteResult.IsSuccess
            ? new CreateProfissionalResponse(result.Value, ConviteEnviado: true, ConviteAviso: null)
            : new CreateProfissionalResponse(result.Value, ConviteEnviado: false, ConviteAviso: inviteResult.Error.Message));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListProfissionaisQuery(includeInactive), ct);
        return Ok(result.Value);
    }
}

public sealed record CreateProfissionalRequest(
    string Nome,
    string Especialidade,
    TipoContrato TipoContrato,
    decimal? PercentualComissaoDefault,
    string? Email,
    Guid? UserId,
    Guid? BranchId);

/// <summary>Envelope de resposta do Create — o profissional já existe mesmo quando o convite falha (ver nota da classe).</summary>
public sealed record CreateProfissionalResponse(ProfissionalDto Profissional, bool ConviteEnviado, string? ConviteAviso);
