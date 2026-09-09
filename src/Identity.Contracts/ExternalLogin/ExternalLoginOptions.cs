namespace StarterKit.Identity.Contracts.ExternalLogin;

public sealed class ExternalLoginOptions
{
    public IReadOnlyList<string> AllowedTenantIds { get; init; } = [];

    public IReadOnlyList<string> AllowedEmailDomains { get; init; } = [];
}
