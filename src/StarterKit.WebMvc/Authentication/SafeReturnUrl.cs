namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Open-redirect guard for <c>returnUrl</c>/<c>state</c> values: only same-site relative paths are
/// kept — the admin client's rule (<c>startsWith("/") &amp;&amp; !startsWith("//")</c>), also rejecting
/// the backslash variant browsers normalize to <c>//</c>.
/// </summary>
public static class SafeReturnUrl
{
    public const string Default = "/";

    public static string Sanitize(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || returnUrl[0] != '/')
        {
            return Default;
        }

        if (returnUrl.Length > 1 && returnUrl[1] is '/' or '\\')
        {
            return Default;
        }

        return returnUrl;
    }
}
