using Light.Mediator;
using Monolith.Identity.Domain.Events;

namespace Monolith.Identity.Application.Users.EventHandlers;

internal class UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    : INotificationHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("User created: {UserId}, {UserName}, {Email}",
            notification.UserId,
            notification.UserName,
            notification.Email);

        return Task.CompletedTask;
    }
}
