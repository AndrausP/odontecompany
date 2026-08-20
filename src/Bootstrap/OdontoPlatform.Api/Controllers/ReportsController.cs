using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Queries.GetAgendaOcupacao;
using Reporting.Application.Queries.GetDashboardResumo;
using Reporting.Application.Queries.GetFaturamentoPorPeriodo;
using Reporting.Application.Queries.GetReceitaPorProcedimento;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Relatórios e dashboards (Fase 4). Admin-only — dados agregados de negócio (financeiro +
/// volume clínico), não é operação do dia a dia de recepção/dentista. Composição EM TEMPO REAL
/// (ver Reporting.Application.Queries.GetDashboardResumo.GetDashboardResumoQueryHandler pro
/// racional) — sem read replica real disponível nesta fase, tudo aponta pro mesmo banco de
/// escrita (dívida técnica documentada em docs/tasks/008-inteligencia-bi.md).
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Owner,Admin", Policy = "RequireActiveOrganization")]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public ReportsController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new GetDashboardResumoQuery(organizationId.Value, dataInicio, dataFim), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet("faturamento")]
    public async Task<IActionResult> GetFaturamento([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new GetFaturamentoPorPeriodoQuery(organizationId.Value, dataInicio, dataFim), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet("agenda")]
    public async Task<IActionResult> GetAgenda([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new GetAgendaOcupacaoQuery(organizationId.Value, dataInicio, dataFim), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Receita de agendamentos concluídos no período, agrupada por procedimento (task 044).</summary>
    [HttpGet("procedimentos")]
    public async Task<IActionResult> GetReceitaPorProcedimento([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new GetReceitaPorProcedimentoQuery(organizationId.Value, dataInicio, dataFim), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }
}
