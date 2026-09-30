using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Published after a user is provisioned. Carries the user's full current state.
/// Consumers must be idempotent on <see cref="UserId"/> and drop events whose
/// <see cref="Version"/> is not newer than the one already applied.
/// </summary>
public sealed record UserProvisionedIntegrationEvent(
    string UserId,
    string Email,
    string UserName,
    string? FirstName,
    string? LastName,
    ProvisioningSource Source,
    long Version)
    : IntegrationEvent;
