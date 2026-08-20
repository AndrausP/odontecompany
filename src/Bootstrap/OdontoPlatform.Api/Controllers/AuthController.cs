using Identity.Application.Commands.ForgotPassword;
using Identity.Application.Commands.Login;
using Identity.Application.Commands.Refresh;
using Identity.Application.Commands.ResetPassword;
using Identity.Application.Commands.Signup;
using Identity.Application.Commands.SwitchOrganization;
using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Autenticação multi-organization.
///
/// Decisão de design: o login NÃO exige que o cliente informe o organization (nem header
/// <c>X-Organization</c>, nem subdomínio). Email é único GLOBALMENTE entre organizations (índice único na
/// tabela Users), então o próprio usuário encontrado pelo email já revela o organization_id — esse é
/// o único ponto do módulo Identity que consulta ignorando o filtro global de organization
/// (ver <c>IUserRepository.GetByEmailAcrossOrganizationsAsync</c>). O token emitido carrega
/// <c>organization_id</c> como claim e passa a escopar todas as requisições autenticadas seguintes
/// via <c>OrganizationMiddleware</c>.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string EmailJaCadastradoCode = "User.EmailJaCadastrado";

    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public AuthController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Signup público (task 015) — revoga a regra antiga "sem registro público". Qualquer pessoa
    /// cria conta sem convite e sem admin. Retorna access token SEM organization (usuário ainda
    /// não pertence a nenhuma) e SEM refresh token (ver <c>SignupResultDto</c>). Email duplicado:
    /// 409 com mensagem genérica, nunca revela se o email já existe (anti-enumeração). Rate limit
    /// fixo por IP (política "signup" — ver Program.cs) contra abuso de superfície anônima.
    /// </summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    [EnableRateLimiting("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (result.IsSuccess)
            return Created($"/api/auth/me", result.Value);

        return result.Error.Code switch
        {
            EmailJaCadastradoCode => Conflict(new { error = result.Error.Message }),
            _ => BadRequest(new { error = result.Error.Message })
        };
    }

    /// <summary>
    /// Autentica por email + senha. Retorna access token (15min) escopado à organization mais
    /// antiga do usuário (eleição automática — task 014) + refresh token (7 dias). Usuário sem
    /// nenhuma organization ainda recebe access token válido SEM claim organization_id e SEM
    /// refresh token (RefreshToken é <c>null</c> nesse caso — ver <c>LoginResultDto</c>).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error.Message });
    }

    /// <summary>
    /// Pede um link de redefinição de senha (auditoria pré-venda — antes disso, não existia
    /// NENHUMA recuperação de senha no produto). Sempre 200, exista ou não o email — anti-
    /// enumeração. Rate limit por IP (mesma política de superfície anônima do signup).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return Ok(new { message = "Se o email existir, enviamos as instruções de redefinição." });
    }

    /// <summary>Conclui a redefinição a partir do token recebido no passo anterior. Revoga todas as sessões ativas do usuário.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(new { message = "Senha redefinida." }) : BadRequest(new { error = result.Error.Message });
    }

    /// <summary>Troca um refresh token válido por um novo par access+refresh (rotação: o token antigo é invalidado).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error.Message });
    }

    /// <summary>
    /// Troca a organization ativa do usuário autenticado SEM refazer login/senha — emite novo
    /// par access+refresh escopado à organization pedida. Não exige <c>RequireActiveOrganization</c>
    /// (usuário pode estar trocando A PARTIR de um token sem org, ex: acabou de aceitar um
    /// convite). OrganizationId vem sempre do sub do token, NUNCA do corpo — impede um usuário de
    /// "trocar" pra uma organization à qual não é afiliado (o handler valida a membership de
    /// qualquer forma, essa é só a primeira camada). Membership inexistente/inativa → 403.
    /// </summary>
    [HttpPost("switch-organization")]
    [Authorize]
    public async Task<IActionResult> SwitchOrganization([FromBody] SwitchOrganizationRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Unauthorized();

        var command = new SwitchOrganizationCommand(userId.Value, request.OrganizationId);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : Forbid();
    }
}

/// <summary>Corpo da requisição de troca de organization ativa.</summary>
public sealed record SwitchOrganizationRequest(Guid OrganizationId);
