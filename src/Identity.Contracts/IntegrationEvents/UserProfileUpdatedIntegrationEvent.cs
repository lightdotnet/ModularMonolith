using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Published after a user's profile changes. Carries the full current profile, not a delta.
/// Consumers must be idempotent on <see cref="UserId"/> and drop events whose
/// <see cref="Version"/> is not newer than the one already applied.
/// </summary>
public sealed record UserProfileUpdatedIntegrationEvent(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    long Version)
    : IntegrationEvent;
