using Contracts.Abstractions.Pagination;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Patients.Application.Commands.CreatePatient;
using Patients.Application.Commands.DeactivatePatient;
using Patients.Application.Commands.UpdatePatient;
using Patients.Application.Queries.GetPatientById;
using Patients.Application.Queries.ListPatients;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// CRUD de pacientes. Qualquer papel autenticado pode ler (GET); criar/editar/desativar é
/// restrito a Admin/Recepção — Dentista não administra cadastro de paciente, é operação de
/// recepção/admin (decisão documentada na task 003).
/// </summary>
[ApiController]
[Route("api/patients")]
[Authorize(Policy = "RequireActiveOrganization")]
public sealed class PatientsController : ControllerBase
{
    private const string PatientNaoEncontradoCode = "Patient.NaoEncontrado";

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public PatientsController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Cria um paciente no organization do usuário autenticado. Exige consentimento LGPD explícito.</summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Admin,Recepcao")]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreatePatientCommand(
            organizationId.Value,
            request.NomeCompleto,
            request.Cpf,
            request.DataNascimento,
            request.Telefone,
            request.Email,
            request.Endereco,
            request.ConsentimentoLgpd);

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Atualiza dados cadastrais. CPF nunca é alterado (é imutável após a criação).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Recepcao")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
    {
        var command = new UpdatePatientCommand(
            id,
            request.NomeCompleto,
            request.DataNascimento,
            request.Telefone,
            request.Email,
            request.Endereco);

        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == PatientNaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Desativa (soft delete) — a linha nunca é removida do banco.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Recepcao")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeactivatePatientCommand(id), ct);

        if (result.IsSuccess)
            return NoContent();

        return result.Error.Code == PatientNaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Busca por id. Qualquer papel autenticado pode ler.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == PatientNaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Listagem paginada. Só pacientes ativos por padrão — use includeInactive=true pra incluir os desativados.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? nome,
        [FromQuery] string? cpf,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken ct = default)
    {
        var pagination = new PageRequest { Page = page, PageSize = pageSize };
        var query = new ListPatientsQuery(pagination, nome, cpf, includeInactive);

        var result = await _mediator.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }
}

/// <summary>Corpo da requisição de criação de paciente — organization nunca é informado aqui (vem do JWT).</summary>
public sealed record CreatePatientRequest(
    string NomeCompleto,
    string Cpf,
    DateTime DataNascimento,
    string Telefone,
    string? Email,
    string? Endereco,
    bool ConsentimentoLgpd);

/// <summary>Corpo da requisição de atualização — sem campo Cpf de propósito (CPF é imutável).</summary>
public sealed record UpdatePatientRequest(
    string NomeCompleto,
    DateTime DataNascimento,
    string Telefone,
    string? Email,
    string? Endereco);
