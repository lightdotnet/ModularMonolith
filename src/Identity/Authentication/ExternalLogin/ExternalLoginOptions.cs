namespace StarterKit.Modules.Identity.Authentication.ExternalLogin;

public sealed class ExternalLoginOptions
{
    public IReadOnlyList<string> AllowedTenantIds { get; init; } = [];

    public IReadOnlyList<string> AllowedEmailDomains { get; init; } = [];
}
