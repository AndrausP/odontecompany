using Billing.Application.Commands.CancelarFatura;
using Billing.Application.Commands.CreateFaturaConvenio;
using Billing.Application.Commands.CreateFaturaFromConsultaConcluida;
using Billing.Application.Commands.CreateFaturaParticular;
using Billing.Application.Commands.RegistrarPagamentoParcela;
using Billing.Application.Queries.GetFaturaById;
using Billing.Application.Queries.ListFaturas;
using Billing.Domain.Enums;
using Contracts.Abstractions.Pagination;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Faturamento particular e por convênio. Restrito a Admin/Recepcao — operação financeira,
/// não clínica (Dentista não administra fatura, mesmo raciocínio de RBAC de Pacientes).
/// </summary>
[ApiController]
[Route("api/faturas")]
[Authorize(Roles = "Owner,Admin,Recepcao", Policy = "RequireActiveOrganization")]
public sealed class FaturasController : ControllerBase
{
    private const string NaoEncontradaCode = "Fatura.NaoEncontrada";

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public FaturasController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("particular")]
    public async Task<IActionResult> CreateParticular([FromBody] CreateFaturaParticularRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        if (!Enum.TryParse<FormaPagamento>(request.FormaPagamento, ignoreCase: true, out var formaPagamento))
            return BadRequest(new { error = "FormaPagamento inválida." });

        var command = new CreateFaturaParticularCommand(
            organizationId.Value, request.PacienteId, request.AgendamentoId, request.ProfissionalId,
            request.ValorTotal, request.NumeroParcelas, formaPagamento, request.ComissaoDentistaPercentual);

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("convenio")]
    public async Task<IActionResult> CreateConvenio([FromBody] CreateFaturaConvenioRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreateFaturaConvenioCommand(
            organizationId.Value, request.PacienteId, request.ConvenioId, request.AgendamentoId,
            request.ProfissionalId, request.ValorTotal, request.ComissaoDentistaPercentual);

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Reconciliação manual — gera fatura a partir de uma consulta concluída sem depender do
    /// worker de Outbox/MassTransit (ainda não implementado, ver
    /// Billing.Infrastructure.Messaging.ConsultaConcluidaEventHandler). Idempotente.
    /// </summary>
    [HttpPost("from-consulta")]
    public async Task<IActionResult> CreateFromConsulta([FromBody] CreateFaturaFromConsultaRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreateFaturaFromConsultaConcluidaCommand(
            request.AgendamentoId, organizationId.Value, request.PacienteId, request.ProfissionalId,
            request.DataHoraConclusao, request.Valor);

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess ? Ok(new { faturaId = result.Value }) : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/parcelas/{parcelaId:guid}/pagamento")]
    public async Task<IActionResult> RegistrarPagamento(Guid id, Guid parcelaId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegistrarPagamentoParcelaCommand(id, parcelaId), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == NaoEncontradaCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelarFaturaCommand(id), ct);

        if (result.IsSuccess)
            return NoContent();

        return result.Error.Code == NaoEncontradaCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFaturaByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? pacienteId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken ct = default)
    {
        var query = new ListFaturasQuery(new PageRequest { Page = page, PageSize = pageSize }, pacienteId, status);
        var result = await _mediator.Send(query, ct);
        return Ok(result.Value);
    }
}

public sealed record CreateFaturaParticularRequest(
    Guid PacienteId,
    Guid? AgendamentoId,
    Guid? ProfissionalId,
    decimal ValorTotal,
    int NumeroParcelas,
    string FormaPagamento,
    decimal? ComissaoDentistaPercentual);

public sealed record CreateFaturaConvenioRequest(
    Guid PacienteId,
    Guid ConvenioId,
    Guid? AgendamentoId,
    Guid? ProfissionalId,
    decimal ValorTotal,
    decimal? ComissaoDentistaPercentual);

public sealed record CreateFaturaFromConsultaRequest(
    Guid AgendamentoId,
    Guid PacienteId,
    Guid ProfissionalId,
    DateTime DataHoraConclusao,
    decimal Valor);
