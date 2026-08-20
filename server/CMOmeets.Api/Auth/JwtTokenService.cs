using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CMOmeets.Application.Auth;
using CMOmeets.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CMOmeets.Api.Auth;

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public (string token, DateTime expiresAt) CreateToken(AppUser user, IList<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("displayName", user.DisplayName)
        };
        if (user.DepartmentId is int deptId)
            claims.Add(new Claim("deptId", deptId.ToString()));
        if (user.OfficerId is int officerId)
            claims.Add(new Claim("officerId", officerId.ToString()));
        // A 'cmo_officer' login carries its directly-chosen department set (CSV of departmentMas.RID).
        if (!string.IsNullOrWhiteSpace(user.DepartmentIds))
            claims.Add(new Claim("deptIds", user.DepartmentIds));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
