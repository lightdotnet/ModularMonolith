namespace StarterKit.Modules.Identity.Infrastructure.Authentication.ExternalLogin;

public sealed class ExternalLoginOptions
{
    public IReadOnlyList<string> AllowedTenantIds { get; init; } = [];

    public IReadOnlyList<string> AllowedEmailDomains { get; init; } = [];
}
