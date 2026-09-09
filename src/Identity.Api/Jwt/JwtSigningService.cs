using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StarterKit.Shared.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StarterKit.Identity.Api.Jwt;

internal class JwtSigningService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public string Issuer => _jwt.Issuer;

    public static string RoleClaimType => ClaimTypeConstants.Role;

    public string Generate(
        IEnumerable<Claim> claims,
        DateTime expiresAt,
        string? audience = null)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal Validate(string token, bool expired = false)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = false,
            RoleClaimType = RoleClaimType,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = !expired,
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken
            || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}
