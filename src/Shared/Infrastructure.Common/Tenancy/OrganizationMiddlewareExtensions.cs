using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Common.Tenancy;

public static class OrganizationMiddlewareExtensions
{
    /// <summary>Registra o <see cref="OrganizationMiddleware"/> no pipeline HTTP.</summary>
    public static IApplicationBuilder UseOrganizationResolution(this IApplicationBuilder app)
        => app.UseMiddleware<OrganizationMiddleware>();
}
