using StarterKit.Modules.Identity.Contracts;
using StarterKit.Modules.Notifications.Domain;
using StarterKit.Modules.Notifications.Persistence;
using StarterKit.Modules.Notifications.SignalR;

namespace StarterKit.Modules.Notifications.Features.Notifications.Commands;

internal sealed record SendNotificationCommand(
    string RecipientUserId,
    SystemMessage Message,
    string? SenderUserId)
    : ICommand<IResult>;

/// <summary>
/// Persists a notification for the recipient, then pushes it live to that user over SignalR.
/// </summary>
internal class SendNotificationCommandHandler(
    NotificationDbContext context,
    IHubService hub,
    IIdentityModuleApi identityApi)
    : ICommandHandler<SendNotificationCommand, IResult>
{
    public async Task<IResult> Handle(
        SendNotificationCommand request,
        CancellationToken cancellationToken)
    {
        string? senderName = null;

        if (!string.IsNullOrEmpty(request.SenderUserId))
        {
            var sender = await identityApi.GetUserAsync(request.SenderUserId, cancellationToken);

            if (sender != null)
            {
                var fullName = $"{sender.FirstName} {sender.LastName}".Trim();

                senderName = fullName.Length > 0
                    ? fullName
                    : sender.UserName;
            }
        }

        var entity = Notification.Create(
            request.RecipientUserId,
            request.Message.Title,
            request.Message.Message,
            request.Message.Url,
            request.SenderUserId,
            senderName);

        await context.Notifications.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Push after the record is saved so a client reacting to the push can load
        // the persisted entry from the API. The payload itself is sent to the client too.
        await hub.SendAsync(
            request.Message,
            request.RecipientUserId,
            cancellationToken);

        return Result.Success();
    }
}
