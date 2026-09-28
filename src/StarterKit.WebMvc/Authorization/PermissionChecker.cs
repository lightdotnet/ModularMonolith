using System.Security.Claims;
using Microsoft.Extensions.Options;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Infrastructure;

namespace StarterKit.WebMvc.Authorization;

/// <summary>
/// Permission checks against the cookie principal's <c>permission</c> claims, with the
/// super-admin bypass — the port of the admin client's <c>lib/shared/authorization.ts</c>.
/// Injectable into views (<c>@inject IPermissionChecker</c>) to show/hide UI.
/// </summary>
public interface IPermissionChecker
{
    bool IsSuperAdmin(ClaimsPrincipal user);

    bool HasPermission(
        ClaimsPrincipal user,
        string permission);

    bool HasAnyPermission(
        ClaimsPrincipal user,
        params IEnumerable<string> permissions);

    bool HasAllPermissions(
        ClaimsPrincipal user,
        params IEnumerable<string> permissions);
}

internal sealed class PermissionChecker(
    IOptionsMonitor<PermissionOptions> options)
    : IPermissionChecker
{
    public bool IsSuperAdmin(ClaimsPrincipal user)
    {
        var userName = user.GetUserName();

        return !string.IsNullOrEmpty(userName)
            && options.CurrentValue.SuperAdminUserNames.Contains(userName, StringComparer.Ordinal);
    }

    public bool HasPermission(
        ClaimsPrincipal user,
        string permission)
    {
        return IsAuthenticated(user)
            && (IsSuperAdmin(user) || user.HasClaim(SessionClaimTypes.Permission, permission));
    }

    public bool HasAnyPermission(
        ClaimsPrincipal user,
        params IEnumerable<string> permissions)
    {
        return IsAuthenticated(user)
            && (IsSuperAdmin(user) || permissions.Any(permission => user.HasClaim(SessionClaimTypes.Permission, permission)));
    }

    public bool HasAllPermissions(
        ClaimsPrincipal user,
        params IEnumerable<string> permissions)
    {
        return IsAuthenticated(user)
            && (IsSuperAdmin(user) || permissions.All(permission => user.HasClaim(SessionClaimTypes.Permission, permission)));
    }

    private static bool IsAuthenticated(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true;
}
