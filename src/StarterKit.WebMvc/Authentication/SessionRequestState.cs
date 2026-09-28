namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Per-request session facts set by <see cref="SessionCookieEvents"/> and read by the backend
/// client pipeline (<c>BearerTokenHandler</c>).
/// </summary>
public static class SessionRequestState
{
    private static readonly object AccessTokenUnavailableKey = new();

    /// <summary>
    /// Marks the session's access token as unusable for this request: it has already expired and
    /// the refresh failed transiently (backend unreachable/5xx). Backend calls then fail fast as
    /// "service unavailable" instead of sending the dead token, whose 401 would sign the user out
    /// over what is only an outage.
    /// </summary>
    public static void MarkAccessTokenUnavailable(HttpContext httpContext)
    {
        httpContext.Items[AccessTokenUnavailableKey] = true;
    }

    public static bool IsAccessTokenUnavailable(HttpContext httpContext) =>
        httpContext.Items.ContainsKey(AccessTokenUnavailableKey);
}
