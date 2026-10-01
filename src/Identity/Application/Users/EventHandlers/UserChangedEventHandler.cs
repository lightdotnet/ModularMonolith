using StarterKit.Modules.Identity.Application.Users.Services;
using StarterKit.Modules.Identity.Domain.Events;

namespace StarterKit.Modules.Identity.Application.Users.EventHandlers;

/// <summary>
/// Reloads the cached user list eagerly once a user change has been saved.
/// </summary>
internal sealed class UserChangedEventHandler(
    IUserQueryService userQuery,
    ILogger<UserChangedEventHandler> logger)
    : INotificationHandler<UserChangedEvent>
{
    public async Task Handle(
        UserChangedEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("User changed; reloading the cached user list.");

        await userQuery.ReloadAsync(cancellationToken).ConfigureAwait(false);
    }
}
