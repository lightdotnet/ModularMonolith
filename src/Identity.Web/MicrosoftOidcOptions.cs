namespace StarterKit.Identity.Web;

/// <summary>
/// The web/OIDC-scheme view of the <c>Authentication:Microsoft</c> configuration section.
/// It deliberately lives in <c>Identity.Web</c> rather than <c>Identity.Contracts</c> because
/// it carries <see cref="ClientSecret"/> and Contracts is a cross-module seam. The
/// email-domain allow-list stays on <c>ExternalLoginOptions</c>, which is consumed by
/// <c>ExternalLoginService</c>.
/// </summary>
internal sealed class MicrosoftOidcOptions
{
    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    public string CallbackPath { get; init; } = "/signin-oidc";

    public IReadOnlyList<string> AllowedTenantIds { get; init; } = [];

    /// <summary>
    /// Microsoft (Entra ID) sign-in is optional — it is wired up only when a client id, a
    /// client secret, and at least one allow-listed tenant id are all configured.
    /// </summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && AllowedTenantIds.Count > 0;
}
