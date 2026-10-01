using StarterKit.Shared.Entities;

namespace StarterKit.Modules.Identity.Domain.Events;

/// <summary>
/// Raised after a user was successfully created, updated or deleted.
/// </summary>
/// <remarks>
/// Published through <see cref="IPublisher"/> only after the change has committed - not through
/// <c>IdentityDbContext</c>'s domain-event dispatch, which runs before the save, so a handler
/// that re-reads users would otherwise see the pre-change state.
/// </remarks>
internal sealed record UserChangedEvent : DomainEvent;
