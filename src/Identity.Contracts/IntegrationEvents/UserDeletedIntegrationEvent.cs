using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Published after a user is deleted.
/// Consumers must be idempotent on <see cref="UserId"/> and drop events whose
/// <see cref="Version"/> is not newer than the one already applied.
/// </summary>
public sealed record UserDeletedIntegrationEvent(string UserId, long Version) : IntegrationEvent;
