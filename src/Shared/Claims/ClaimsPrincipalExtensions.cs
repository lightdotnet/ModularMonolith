using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;

namespace Monolith.Claims;

public static class ClaimsPrincipalExtensions
{
    private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType) =>
        principal is null
            ? throw new ArgumentNullException(nameof(principal))
            : principal.FindFirst(claimType)?.Value;

    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.UserId);

    public static string? GetUserName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.UserName);

    public static string? GetFullName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.FullName);

    public static string? GetFirstName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.FirstName);

    public static string? GetLastName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.LastName);

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email);

    public static string? GetPhoneNumber(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.PhoneNumber);

    public static DateTimeOffset GetExpiration(this ClaimsPrincipal principal) =>
        DateTimeOffset.FromUnixTimeSeconds(
            Convert.ToInt64(principal.FindFirstValue(ClaimTypes.Expiration)));

    public static bool IsAuthenticated(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated is true;

    public static bool HasClaim(this ClaimsPrincipal principal, string claimType, string claimValue) =>
        principal.HasClaim(claimType, claimValue) is true;

    public static bool HasPermission(this ClaimsPrincipal principal, string permission) =>
        principal.HasClaim(ClaimTypes.Permission, permission) is true;
}