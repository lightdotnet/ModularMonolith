namespace StarterKit.Identity.Contracts.Users;

/// <summary>
/// Raised once, immediately after Identity just-in-time provisions a local user from an
/// external identity provider (no password, no roles). Consumers send an SSO-appropriate
/// welcome — no credential block. Delivery is best-effort-immediate.
/// </summary>
public sealed record ExternalUserProvisionedIntegrationEvent(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    AuthProvider Provider) : INotification;
