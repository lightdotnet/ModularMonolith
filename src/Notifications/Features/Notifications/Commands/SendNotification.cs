using StarterKit.Modules.Notifications.Domain;
using StarterKit.Modules.Notifications.Persistence;
using StarterKit.Modules.Notifications.SignalR;

namespace StarterKit.Modules.Notifications.Features.Notifications.Commands;

internal sealed record SendNotificationCommand(
    string FromUserId,
    string? FromName,
    string ToUserId,
    SystemMessage Message)
    : ICommand<IResult>;

/// <summary>
/// Persists a notification for the recipient, then pushes it live to that user over SignalR.
/// </summary>
internal class SendNotificationCommandHandler(
    NotificationDbContext context,
    IHubService hub)
    : ICommandHandler<SendNotificationCommand, IResult>
{
    public async Task<IResult> Handle(
        SendNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var entity = Notification.Create(
            request.FromUserId,
            request.FromName,
            request.ToUserId,
            request.Message.Title,
            request.Message.Message,
            request.Message.Url);

        await context.Notifications.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Push after the record is saved so a client reacting to the push can load
        // the persisted entry from the API. The payload itself is sent to the client too.
        await hub.SendAsync(
            request.Message,
            request.ToUserId,
            cancellationToken);

        return Result.Success();
    }
}
