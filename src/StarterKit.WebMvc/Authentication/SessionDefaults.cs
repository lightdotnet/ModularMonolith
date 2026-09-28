namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Cookie-session constants. The lifetime values mirror the admin client
/// (<c>clients/admin/src/lib/server/session-cookie.ts</c>) so both front ends behave the same.
/// </summary>
public static class SessionDefaults
{
    public const string AuthenticationScheme = "WebMvcSession";

    public const string CookieName = "webmvc_session";

    public const string LoginPath = "/Account/Login";

    public const string LogoutPath = "/Account/Logout";

    public const string AccessDeniedPath = "/Account/AccessDenied";

    /// <summary>Hard cap on session lifetime, counted from sign-in — not extended by refresh (admin <c>SESSION_TTL_MS</c>).</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    /// <summary>How long before access-token expiry the session is refreshed (admin <c>REFRESH_LEAD_MS</c>).</summary>
    public static readonly TimeSpan RefreshLead = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Permanent (401/400) refresh-failure count, as stored in the cookie, at which the session is
    /// treated as dead (admin <c>MAX_REFRESH_FAILURES</c>). A failure no longer reissues the cookie
    /// (see <see cref="SessionCookieEvents"/>), so a dead session normally ends on the backend's 401
    /// once the access token expires.
    /// </summary>
    public const int MaxRefreshFailures = 3;

    // AuthenticationProperties token names (StoreTokens/GetTokenValue).
    public const string AccessTokenName = "access_token";

    public const string RefreshTokenName = "refresh_token";

    public const string AccessTokenExpiresAtName = "expires_at";

    // AuthenticationProperties item keys.
    public const string SessionExpiresAtItem = ".session_expires_at";

    public const string RefreshFailureCountItem = ".refresh_failures";
}
