using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Identity.Contracts;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure.Security;

/// <summary>Implementação de <see cref="ICurrentUserAccessor"/> lendo claims do HttpContext atual.</summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId => Guid.TryParse(User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    public Guid? OrganizationId => Guid.TryParse(User?.FindFirst("organization_id")?.Value, out var id) ? id : null;

    public string? Role => User?.FindFirst("role")?.Value;

    public Guid? BranchId => Guid.TryParse(User?.FindFirst("branch_id")?.Value, out var id) ? id : null;
}
