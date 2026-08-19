using Identity.Application.Commands.SkipOnboarding;
using Identity.Application.Queries.GetMe;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Perfil escopado por membership (task 017). Uma chamada devolve tudo que o frontend precisa
/// pra montar cabeçalho e seletor de organization no boot da app: dados do usuário, organizations
/// a que pertence, qual está ativa no token atual e convites pendentes.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize] // Sem RequireActiveOrganization de propósito: tolera token sem organization ativa.
public sealed class MeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public MeController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetMeQuery(userId.Value, _currentUser.OrganizationId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Usuário sem organization escolheu "por enquanto não" no onboarding (sprint-11) — flag
    /// persistida, sem RequireActiveOrganization de propósito (é chamado exatamente por quem
    /// ainda não tem organization nenhuma).
    /// </summary>
    [HttpPost("skip-onboarding")]
    public async Task<IActionResult> SkipOnboarding(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new SkipOnboardingCommand(userId.Value), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error.Message });
    }
}
