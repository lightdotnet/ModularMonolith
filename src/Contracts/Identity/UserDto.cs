using Monolith.Claims;
using System.Security.Claims;
using ClaimTypes = Monolith.Claims.ClaimTypes;

namespace Monolith.Identity;

public class UserDto
{
    public string Id { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public IdentityStatus Status { get; set; }

    public string? AuthProvider { get; set; }

    public bool IsDeleted { get; set; }

    public IList<string> Roles { get; set; } = [];

    public IList<ClaimDto> Claims { get; set; } = [];

    public List<Claim> BuildClaims()
    {
        var claims = new List<Claim>()
        {
            { ClaimTypes.UserId, Id },
            { ClaimTypes.UserName, UserName },
            { ClaimTypes.FirstName, FirstName },
            { ClaimTypes.LastName, LastName },
            { ClaimTypes.PhoneNumber, PhoneNumber },
            { ClaimTypes.Email, Email },
        };

        foreach (var role in Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var claim in Claims)
        {
            claims.Add(new Claim(claim.Type, claim.Value));
        }

        return claims;
    }
}