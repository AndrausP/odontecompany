using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Interfaces;
using Identity.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, int ExpiresInSeconds) GenerateAccessToken(Guid userId, Guid? organizationId, Role? role, Guid? branchId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // organization_id/role (task 014): ausentes quando o usuário não tem organization ativa
        // eleita (zero memberships) — não null/vazio, a claim em si não existe no token, pra
        // RequireClaim("organization_id") (policy RequireActiveOrganization) falhar fechado.
        if (organizationId is not null)
            claims.Add(new Claim("organization_id", organizationId.Value.ToString()));

        if (role is not null)
            claims.Add(new Claim("role", role.Value.ToString()));

        // branch_id (Fase 5) só entra na claim se a membership eleita estiver escopada a uma
        // branch — admin de rede (branchId null) não carrega essa claim, o que
        // ICurrentUserAccessor.BranchId já trata corretamente como "vê todas as branches".
        if (branchId is not null)
            claims.Add(new Claim("branch_id", branchId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresInSeconds = _settings.AccessTokenExpirationMinutes * 60;

        return (tokenString, expiresInSeconds);
    }
}
