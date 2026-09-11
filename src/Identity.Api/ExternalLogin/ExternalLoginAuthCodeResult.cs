using StarterKit.Identity.Contracts;

namespace StarterKit.Identity.Api.ExternalLogin;

public sealed record ExternalLoginAuthCodeResult
{
    public required ExternalLoginAuthCodeStatus Status { get; init; }

    public TokenDto? Token { get; init; } // Status == Success
}

public enum ExternalLoginAuthCodeStatus
{
    Success,

    // Not-found, expired, and PKCE-mismatch all collapse into this single status - the caller
    // must not be able to tell them apart, matching AuthenticationService.RefreshTokenAsync's
    // precedent of collapsing every "bad bearer artifact" case into one Unauthorized shape.
    Failed,
}
