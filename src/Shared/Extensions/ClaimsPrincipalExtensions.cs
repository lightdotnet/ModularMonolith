using StarterKit.Shared.Constants;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;

namespace StarterKit.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    static string? FindFirstValue(this ClaimsPrincipal principal, string claimType) =>
        principal is null
            ? throw new ArgumentNullException(nameof(principal))
            : principal.FindFirst(claimType)?.Value;

    public static string? GetSessionId(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.TokenId);

    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.UserId);

    public static string? GetUserName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.UserName);

    public static string? GetFullName(this ClaimsPrincipal principal)
    {
        var name = $"{principal.GetFirstName()} {principal.GetLastName()}".Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    public static string? GetFirstName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.FirstName);

    public static string? GetLastName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.LastName);

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.Email);

    public static string? GetPhoneNumber(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.PhoneNumber);

    public static string? GetEmployeeId(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypeConstants.EmployeeId);

    public static DateTimeOffset GetExpiration(this ClaimsPrincipal principal) =>
        DateTimeOffset.FromUnixTimeSeconds(
            Convert.ToInt64(principal.FindFirstValue(ClaimTypeConstants.Expiration)));

    public static bool IsAuthenticated(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated is true;

    public static bool HasPermission(this ClaimsPrincipal principal, string permission) =>
        principal.HasClaim(ClaimTypeConstants.Permission, permission) is true;
}
