using Contracts.Abstractions.Pagination;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Application.Commands.AddEvolucaoClinica;
using Records.Application.Commands.CreateProntuario;
using Records.Application.Commands.UploadAnexo;
using Records.Application.Queries.GetProntuarioByPacienteId;
using Records.Application.Queries.ListAuditLog;
using Records.Domain.Enums;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Prontuário eletrônico — dado clínico sensível (LGPD). Restrito a Admin/Dentista: Recepcao
/// não tem acesso a dado clínico (diferente de Patients, onde Recepcao administra cadastro).
/// Toda leitura de prontuário é registrada na trilha de auditoria (não opcional). View da
/// trilha de auditoria em si é Admin-only (compliance).
/// </summary>
[ApiController]
[Route("api/prontuarios")]
[Authorize(Roles = "Owner,Admin,Dentista", Policy = "RequireActiveOrganization")]
public sealed class RecordsController : ControllerBase
{
    private const string NaoEncontradoCode = "Prontuario.NaoEncontrado";
    private const long TamanhoMaximoAnexoBytes = 20 * 1024 * 1024; // 20MB — mesmo limite de AnexoMetadata.TamanhoMaximoBytes

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public RecordsController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Cria o prontuário de um paciente (relação 1:1) — falha se já existir um pro mesmo paciente.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProntuarioRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        var userId = _currentUser.UserId;
        if (organizationId is null || userId is null)
            return Unauthorized(new { error = "Organization ou usuário não resolvido a partir do token." });

        var result = await _mediator.Send(new CreateProntuarioCommand(organizationId.Value, request.PacienteId, userId.Value), ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByPaciente), new { pacienteId = request.PacienteId }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Adiciona uma entrada de evolução clínica — imutável após criada, nunca editada/removida.</summary>
    [HttpPost("{id:guid}/evolucoes")]
    public async Task<IActionResult> AddEvolucao(Guid id, [FromBody] AddEvolucaoRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        var userId = _currentUser.UserId;
        if (organizationId is null || userId is null)
            return Unauthorized(new { error = "Organization ou usuário não resolvido a partir do token." });

        if (!Enum.TryParse<TipoProcedimento>(request.TipoProcedimento, ignoreCase: true, out var tipo))
            return BadRequest(new { error = "TipoProcedimento inválido." });

        var command = new AddEvolucaoClinicaCommand(organizationId.Value, id, userId.Value, tipo, request.DescricaoClinica);
        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == NaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Upload de anexo (raio-x, exame) — multipart/form-data, campo "arquivo".</summary>
    [HttpPost("{id:guid}/anexos")]
    [RequestSizeLimit(TamanhoMaximoAnexoBytes)]
    public async Task<IActionResult> UploadAnexo(Guid id, IFormFile arquivo, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        var userId = _currentUser.UserId;
        if (organizationId is null || userId is null)
            return Unauthorized(new { error = "Organization ou usuário não resolvido a partir do token." });

        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { error = "Arquivo vazio ou não enviado." });

        await using var stream = arquivo.OpenReadStream();
        var command = new UploadAnexoCommand(
            organizationId.Value, id, userId.Value, arquivo.FileName, arquivo.ContentType, arquivo.Length, stream);

        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == NaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Lê o prontuário completo de um paciente (odontograma, evoluções, anexos). Registra acesso na trilha de auditoria.</summary>
    [HttpGet("paciente/{pacienteId:guid}")]
    public async Task<IActionResult> GetByPaciente(Guid pacienteId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized(new { error = "Usuário não resolvido a partir do token." });

        var result = await _mediator.Send(new GetProntuarioByPacienteIdQuery(pacienteId, userId.Value), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error.Message });
    }

    /// <summary>Trilha de auditoria de acesso — Admin-only (compliance), paginada.</summary>
    [HttpGet("{id:guid}/auditoria")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> GetAuditLog(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = PageRequest.DefaultPageSize, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListAuditLogQuery(id, new PageRequest { Page = page, PageSize = pageSize }), ct);
        return Ok(result.Value);
    }
}

public sealed record CreateProntuarioRequest(Guid PacienteId);

public sealed record AddEvolucaoRequest(string TipoProcedimento, string DescricaoClinica);
