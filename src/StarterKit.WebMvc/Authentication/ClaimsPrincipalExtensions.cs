using System.Security.Claims;

namespace StarterKit.WebMvc.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(SessionClaimTypes.UserId);

    public static string? GetUserName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(SessionClaimTypes.UserName);

    /// <summary>Full name, else first + last name, else the username; empty when none is known.</summary>
    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        var fullName = principal.FindFirstValue(SessionClaimTypes.FullName);

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        var name = string.Join(
            ' ',
            new[]
            {
                principal.FindFirstValue(SessionClaimTypes.FirstName),
                principal.FindFirstValue(SessionClaimTypes.LastName),
            }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        return !string.IsNullOrWhiteSpace(name)
            ? name
            : principal.GetUserName() ?? string.Empty;
    }

    /// <summary>Permissions decoded from the access token's <c>permission</c> claim.</summary>
    public static IReadOnlyList<string> GetPermissions(this ClaimsPrincipal principal) =>
        principal.FindAll(SessionClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>Roles decoded from the access token's <c>role</c> claim.</summary>
    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal principal) =>
        principal.FindAll(SessionClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
