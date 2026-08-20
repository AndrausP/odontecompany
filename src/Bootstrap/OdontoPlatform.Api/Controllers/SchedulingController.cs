using Contracts.Abstractions.Pagination;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Commands.CancelarAgendamento;
using Scheduling.Application.Commands.ConfirmarAgendamento;
using Scheduling.Application.Commands.CreateAgendamento;
using Scheduling.Application.Commands.MarcarAgendamentoConcluido;
using Scheduling.Application.Queries.GetAgendamentoById;
using Scheduling.Application.Queries.ListAgendamentos;
using Scheduling.Application.Queries.ListarDisponibilidade;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Agenda de consultas. Diferente do módulo Patients (onde Dentista não administra cadastro),
/// aqui Dentista PODE gerenciar sua própria agenda — decisão do Dev Backend (documentada):
/// criar/confirmar/cancelar/concluir é liberado pra Admin/Recepcao/Dentista.
/// </summary>
[ApiController]
[Route("api/agendamentos")]
[Authorize(Policy = "RequireActiveOrganization")]
public sealed class SchedulingController : ControllerBase
{
    private const string AgendamentoNaoEncontradoCode = "Agendamento.NaoEncontrado";
    private const string SemPermissaoAgendaAlheiaCode = "Agendamento.SemPermissaoAgendaAlheia";

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public SchedulingController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Cria um agendamento (status inicial: Agendado) no organization do usuário autenticado.</summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Admin,Recepcao,Dentista")]
    public async Task<IActionResult> Create([FromBody] CreateAgendamentoRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreateAgendamentoCommand(
            organizationId.Value, request.PacienteId, request.ProfissionalId, request.SalaId, request.Inicio, request.Fim, request.ProcedimentoId);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Agendado → Confirmado. Lock curto no Redis + revalidação de sobreposição + token otimista (xmin).</summary>
    [HttpPost("{id:guid}/confirmar")]
    [Authorize(Roles = "Owner,Admin,Recepcao,Dentista")]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmarAgendamentoCommand(id, _currentUser.UserId, _currentUser.Role), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code switch
        {
            AgendamentoNaoEncontradoCode => NotFound(new { error = result.Error.Message }),
            SemPermissaoAgendaAlheiaCode => Forbid(),
            _ => BadRequest(new { error = result.Error.Message })
        };
    }

    /// <summary>Agendado/Confirmado → Cancelado.</summary>
    [HttpPost("{id:guid}/cancelar")]
    [Authorize(Roles = "Owner,Admin,Recepcao,Dentista")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarAgendamentoRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelarAgendamentoCommand(id, request?.Motivo, _currentUser.UserId, _currentUser.Role), ct);

        if (result.IsSuccess)
            return NoContent();

        return result.Error.Code switch
        {
            AgendamentoNaoEncontradoCode => NotFound(new { error = result.Error.Message }),
            SemPermissaoAgendaAlheiaCode => Forbid(),
            _ => BadRequest(new { error = result.Error.Message })
        };
    }

    /// <summary>Confirmado → Concluido. Dispara ConsultaConcluidaEvent via Outbox.</summary>
    [HttpPost("{id:guid}/concluir")]
    [Authorize(Roles = "Owner,Admin,Recepcao,Dentista")]
    public async Task<IActionResult> Concluir(Guid id, [FromBody] ConcluirAgendamentoRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarcarAgendamentoConcluidoCommand(id, request.Valor, _currentUser.UserId, _currentUser.Role), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code switch
        {
            AgendamentoNaoEncontradoCode => NotFound(new { error = result.Error.Message }),
            SemPermissaoAgendaAlheiaCode => Forbid(),
            _ => BadRequest(new { error = result.Error.Message })
        };
    }

    /// <summary>Busca por id. Qualquer papel autenticado pode ler.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgendamentoByIdQuery(id), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == AgendamentoNaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Listagem paginada com filtros opcionais (profissional, paciente, status, intervalo de
    /// data). Fase 5: Recepcao só enxerga agendamentos da própria branch — forçado aqui a
    /// partir de <c>ICurrentUserAccessor.BranchId</c>, nunca aceito como input do cliente (se
    /// fosse query string, bastaria omitir o parâmetro pra ver a rede inteira). Admin e
    /// Dentista não são restritos por branch.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? profissionalId,
        [FromQuery] Guid? pacienteId,
        [FromQuery] string? status,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken ct = default)
    {
        var branchId = _currentUser.Role == "Recepcao" ? _currentUser.BranchId : null;

        var pagination = new PageRequest { Page = page, PageSize = pageSize };
        var query = new ListAgendamentosQuery(pagination, profissionalId, pacienteId, status, dataInicio, dataFim, branchId);

        var result = await _mediator.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Slots livres de um profissional num dia — cache-aside (TTL 1h) por trás.</summary>
    [HttpGet("disponibilidade")]
    public async Task<IActionResult> Disponibilidade([FromQuery] Guid profissionalId, [FromQuery] DateOnly data, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var result = await _mediator.Send(new ListarDisponibilidadeQuery(organizationId.Value, profissionalId, data), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }
}

/// <summary>Corpo da requisição de criação — organization nunca é informado aqui (vem do JWT).</summary>
public sealed record CreateAgendamentoRequest(Guid PacienteId, Guid ProfissionalId, Guid SalaId, DateTime Inicio, DateTime Fim, Guid? ProcedimentoId = null);

public sealed record CancelarAgendamentoRequest(string? Motivo);

public sealed record ConcluirAgendamentoRequest(decimal Valor);
