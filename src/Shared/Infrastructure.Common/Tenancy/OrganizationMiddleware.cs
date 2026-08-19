using Microsoft.AspNetCore.Http;

namespace Infrastructure.Common.Tenancy;

/// <summary>
/// Resolve o organization da requisição autenticada a partir da claim <c>organization_id</c> do JWT e
/// popula o <see cref="IOrganizationContext"/> (scoped), consumido pelos filtros globais do EF Core.
/// Precisa rodar DEPOIS de UseAuthentication (senão HttpContext.User ainda não tem claims) e
/// ANTES de UseAuthorization/MapControllers.
///
/// Requisições anônimas (login, refresh) não têm organization resolvido aqui — nesses casos o
/// próprio Use Case resolve o organization via lookup de usuário por email/token, ignorando o
/// filtro global (única exceção documentada no módulo Identity).
/// </summary>
public sealed class OrganizationMiddleware
{
    public const string OrganizationClaimType = "organization_id";

    private readonly RequestDelegate _next;

    public OrganizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IOrganizationContext organizationContext)
    {
        var claim = context.User?.FindFirst(OrganizationClaimType);
        if (claim is not null && Guid.TryParse(claim.Value, out var organizationId))
        {
            organizationContext.SetOrganization(organizationId);
        }

        await _next(context);
    }
}
