using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Servit.Domain.Entities;

namespace Servit.Api.Services;

public class JwtTokenService(IConfiguration configuration)
{
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(ApplicationUser user, IList<string> roles)
    {
        var key = configuration["Jwt:Key"]!;
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        // Admin tokens expire in 8 hours (one work session);
        // regular (mobile) tokens last 7 days to avoid frequent re-login.
        var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        var expiresAt = isAdmin
            ? DateTimeOffset.UtcNow.AddHours(8)
            : DateTimeOffset.UtcNow.AddDays(7);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
