namespace StarterKit.Host.Authentication;

/// <summary>
/// Host-local binding of the SignalR hub path (<c>Notifications:Hub</c>) used by the
/// authentication pipeline to route hub requests to the <c>HubBearer</c> scheme and to keep
/// hub tokens out of the JSON API.
/// </summary>
/// <remarks>
/// TODO: this is a stand-in until the Notifications module exists on this branch. When it
/// does, switch to that module's hub options (and its path validation) so the hub itself and
/// this authentication pipeline bind and validate one identical value, then delete this type.
/// </remarks>
internal sealed class HubOptions
{
    public const string SectionName = "Notifications:Hub";

    public const string InvalidPathMessage =
        "Configuration value 'Notifications:Hub:Path' is invalid. It must be a non-empty absolute path "
        + "starting with '/', without a trailing '/', and must not contain whitespace, a query string ('?') "
        + "or a fragment ('#').";

    public string Path { get; set; } = "/signalr-hub";

    /// <summary>
    /// A valid hub path is non-empty, starts with <c>'/'</c>, has no trailing <c>'/'</c>
    /// (which also rules out the bare root <c>"/"</c>), and contains no whitespace, query
    /// (<c>'?'</c>) or fragment (<c>'#'</c>) — i.e. a value <see cref="PathString"/> segment
    /// matching can use as-is.
    /// </summary>
    public static bool IsValidHubPath(string? path) =>
        !string.IsNullOrEmpty(path)
        && path[0] == '/'
        && path[^1] != '/'
        && !path.Any(char.IsWhiteSpace)
        && path.IndexOfAny(['?', '#']) < 0;
}
