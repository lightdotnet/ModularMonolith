namespace StarterKit.Identity.Web.Pages.Account;

/// <summary>
/// <see cref="Microsoft.AspNetCore.Authentication.AuthenticationProperties.Items"/> keys used to
/// carry the relay's client-supplied parameters (<c>redirectUri</c>/<c>state</c>/<c>codeChallenge</c>)
/// from <see cref="ExternalLoginStartModel"/> through the Entra ID round trip to
/// <see cref="ExternalLoginRelayModel"/>.
/// </summary>
internal static class ExternalLoginRelayItemKeys
{
    public const string RedirectUri = "relay.redirect_uri";

    public const string State = "relay.state";

    public const string CodeChallenge = "relay.code_challenge";
}
