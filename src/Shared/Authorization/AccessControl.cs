using System.Security.Claims;

namespace Monolith.Authorization;

public static class AccessControl
{
    public static bool IsFullControl(this ICurrentUser currentUser) =>
        AppSecret.IsSuper(currentUser.Username);

    public static bool IsFullControl(this ClaimsPrincipal? claimsPrincipal) =>
        AppSecret.IsSuper(claimsPrincipal?.GetUserName());
}
