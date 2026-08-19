using Contracts.Abstractions.Pagination;
using Estoque.Application.Commands.CreateItemEstoque;
using Estoque.Application.Commands.RegistrarEntrada;
using Estoque.Application.Commands.RegistrarSaida;
using Estoque.Application.Queries.ListItensEstoque;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Estoque de materiais/insumos odontológicos — módulo transversal (doc de arquitetura,
/// "fase posterior"). Admin+Recepcao administram (operação do dia a dia de baixa/reposição de
/// material); Dentista não (mesmo raciocínio de Pacientes/Faturas).
/// </summary>
[ApiController]
[Route("api/estoque")]
[Authorize(Roles = "Owner,Admin,Recepcao", Policy = "RequireActiveOrganization")]
public sealed class EstoqueController : ControllerBase
{
    private const string NaoEncontradoCode = "ItemEstoque.NaoEncontrado";

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public EstoqueController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemEstoqueRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreateItemEstoqueCommand(organizationId.Value, request.BranchId, request.Nome, request.UnidadeMedida, request.QuantidadeMinima);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/entrada")]
    public async Task<IActionResult> RegistrarEntrada(Guid id, [FromBody] MovimentacaoRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegistrarEntradaCommand(id, request.Quantidade), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == NaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("{id:guid}/saida")]
    public async Task<IActionResult> RegistrarSaida(Guid id, [FromBody] MovimentacaoRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegistrarSaidaCommand(id, request.Quantidade), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code == NaoEncontradoCode
            ? NotFound(new { error = result.Error.Message })
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? branchId,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken ct = default)
    {
        var query = new ListItensEstoqueQuery(new PageRequest { Page = page, PageSize = pageSize }, branchId, includeInactive);
        var result = await _mediator.Send(query, ct);

        return Ok(result.Value);
    }
}

public sealed record CreateItemEstoqueRequest(Guid? BranchId, string Nome, string UnidadeMedida, decimal QuantidadeMinima);

public sealed record MovimentacaoRequest(decimal Quantidade);
