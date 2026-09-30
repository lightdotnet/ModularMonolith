namespace StarterKit.Modules.Identity.Contracts.IntegrationEvents;

/// <summary>
/// How a user came to exist in the Identity module.
/// </summary>
public enum ProvisioningSource
{
    /// <summary>
    /// Created explicitly: by an administrator, or on another module's behalf through
    /// <see cref="IIdentityModuleApi.EnsureUserAsync"/>.
    /// </summary>
    Admin,

    /// <summary>
    /// Created just-in-time on the user's first sign-in through an external identity provider.
    /// </summary>
    ExternalLogin,
}
