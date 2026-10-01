using StarterKit.Modules.Identity.Contracts.IntegrationEvents;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Application.Users.IntegrationEvents;

/// <summary>
/// Builds the Identity integration events from a <see cref="User"/>'s current state.
/// </summary>
/// <remarks>
/// The user entity has no monotonic version of its own (its audit timestamps are stamped only
/// while saving, after the event is built), so <see cref="NextVersion"/> derives the version
/// from the clock in Unix milliseconds.
/// </remarks>
internal static class UserIntegrationEvents
{
    public static long NextVersion(IDateTime clock) => clock.UtcNow.ToUnixTimeMilliseconds();

    public static UserProvisionedIntegrationEvent Provisioned(
        User user,
        ProvisioningSource source,
        long version) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.FirstName,
            user.LastName,
            source,
            version);

    public static UserProfileUpdatedIntegrationEvent ProfileUpdated(
        User user,
        long version) =>
        new(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, version);

    public static UserStatusChangedIntegrationEvent StatusChanged(
        User user,
        long version) =>
        new(user.Id, user.Status.IsActive, version);

    public static UserDeletedIntegrationEvent Deleted(
        string userId,
        long version) =>
        new(userId, version);
}
