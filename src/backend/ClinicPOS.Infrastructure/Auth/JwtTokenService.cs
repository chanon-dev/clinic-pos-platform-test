using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicPOS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicPOS.Infrastructure.Auth;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "ClinicPOS-SuperSecret-Key-2026-Min32Chars!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(12);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenantId", user.TenantId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        foreach (var ub in user.UserBranches)
        {
            claims.Add(new Claim("branchIds", ub.BranchId.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "ClinicPOS",
            audience: _config["Jwt:Audience"] ?? "ClinicPOS",
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
