using Identity.Application.Commands.CreateUser;
using Identity.Contracts;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OdontoPlatform.Api.Controllers;

/// <summary>
/// Gestão de usuários dentro de uma organization — Admin provisiona Dentista/Recepção/Admin
/// dentro da PRÓPRIA organization. Não confundir com o signup público (task 015,
/// <c>POST /api/auth/signup</c>): aquele cria um usuário GLOBAL sem organization nenhuma; este
/// endpoint sempre cria usuário JÁ afiliado à organization do Admin autenticado.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Owner,Admin", Policy = "RequireActiveOrganization")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;

    public UsersController(IMediator mediator, ICurrentUserAccessor currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Admin autenticado cria um usuário (Dentista/Recepção/Admin) dentro do PRÓPRIO organization.
    /// O organization_id nunca vem do corpo da requisição — sempre da claim do JWT — pra impedir
    /// que um Admin crie usuário em outro organization.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            return Unauthorized(new { error = "Organization não resolvido a partir do token." });

        var command = new CreateUserCommand(organizationId.Value, request.Nome, request.Email, request.Password, request.Role, request.BranchId);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Create), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error.Message });
    }
}

/// <summary>Corpo da requisição de criação de usuário — organization nunca é informado aqui (vem do JWT). BranchId (Fase 5) é opcional.</summary>
public sealed record CreateUserRequest(string Nome, string Email, string Password, Role Role, Guid? BranchId = null);
