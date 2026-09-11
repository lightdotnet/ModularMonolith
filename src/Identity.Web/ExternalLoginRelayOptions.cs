namespace StarterKit.Identity.Web;

/// <summary>
/// Configuration for the OAuth-style authorization-code relay (<c>Account/ExternalLoginStart</c> +
/// <c>Account/ExternalLoginRelay</c>) that lets a separate-origin client (e.g. the Next.js admin
/// app) complete Microsoft sign-in without ever holding an <c>Identity.Web</c> cookie session.
/// </summary>
public sealed class ExternalLoginRelayOptions
{
    public int AuthCodeTtlSeconds { get; set; } = 90;

    /// <summary>
    /// Exact-match allow-list of client callback URLs the relay is permitted to redirect back to.
    /// No prefix/substring matching - <c>redirectUri</c> must equal one of these entries exactly.
    /// </summary>
    public string[] AllowedRedirectUris { get; set; } = [];
}
