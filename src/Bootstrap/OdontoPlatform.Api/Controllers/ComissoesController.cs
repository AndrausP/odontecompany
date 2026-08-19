using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Queries.GetComissoesPorPeriodo;
using Scheduling.Contracts;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Relatório de comissões (Fase 4, task 023). Controller SEPARADO de
/// <see cref="ReportsController"/> de propósito (D4): <see cref="ReportsController"/> é
/// <c>[Authorize(Roles = "Admin")]</c> na CLASSE, e em ASP.NET Core múltiplos <c>[Authorize]</c>
/// são cumulativos (AND) — um <c>[Authorize(Roles = "Dentista")]</c> no método não afrouxaria o
/// da classe, o Dentista continuaria bloqueado. Aqui a policy de roles já nasce certa:
/// Owner/Admin veem o agregado da organization, Dentista vê só a própria linha, Recepcao nem
/// entra (403 automático do framework — não figura na lista de <see cref="Authorize"/>).
/// </summary>
[ApiController]
[Route("api/reports/comissoes")]
[Authorize(Roles = "Owner,Admin,Dentista", Policy = "RequireActiveOrganization")]
public sealed class ComissoesController : ControllerBase
{
    // Nenhum Profissional.Id real é Guid.Empty (BaseEntity gera via Guid.NewGuid()) — usado como
    // filtro impossível de propósito quando o Dentista logado não tem Profissional vinculado
    // nesta organization: a query roda normal (valida o período do mesmo jeito) e devolve 200 com
    // totais zerados e lista vazia, sem duplicar a regra de validação de período aqui no controller.
    private static readonly Guid ProfissionalNaoVinculadoSentinel = Guid.Empty;

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IProfissionalLookup _profissionalLookup;

    public ComissoesController(IMediator mediator, ICurrentUserAccessor currentUser, IProfissionalLookup profissionalLookup)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _profissionalLookup = profissionalLookup;
    }

    /// <summary>
    /// Comissão por profissional no período. Owner/Admin recebem o agregado da organization
    /// (com recorte opcional por <paramref name="branchId"/>/<paramref name="classe"/>); Dentista
    /// recebe SEMPRE só a própria linha — <paramref name="profissionalId"/>/<paramref name="branchId"/>/
    /// <paramref name="classe"/> enviados por um Dentista são ignorados (D5: escopo resolvido no
    /// controller a partir do token, nunca aceito do cliente — mesmo padrão de
    /// <see cref="SchedulingController.List"/>).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComissoes(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim,
        [FromQuery] Guid? branchId,
        [FromQuery] string? classe,
        [FromQuery] Guid? profissionalId,
        CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        if (_currentUser.Role == "Dentista")
        {
            var profissionais = await _profissionalLookup.ListarPorOrganizationAsync(organizationId.Value, ct);
            var proprioProfissional = profissionais.FirstOrDefault(p => p.UserId == _currentUser.UserId);

            profissionalId = proprioProfissional?.Id ?? ProfissionalNaoVinculadoSentinel;
            branchId = null;
            classe = null;
        }

        var query = new GetComissoesPorPeriodoQuery(organizationId.Value, dataInicio, dataFim, branchId, classe, profissionalId);
        var result = await _mediator.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }
}
