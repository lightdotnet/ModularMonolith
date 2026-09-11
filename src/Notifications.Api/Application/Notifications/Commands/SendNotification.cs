using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.Application.Notifications.Commands;

internal sealed record SendNotificationCommand(
    string FromUserId,
    string? FromName,
    string ToUserId,
    SystemMessage Message) : ICommand<IResult>;

internal class SendNotificationCommandHandler(
    INotificationService notificationService)
    : ICommandHandler<SendNotificationCommand, IResult>
{
    public async Task<IResult> Handle(
        SendNotificationCommand request,
        CancellationToken cancellationToken)
    {
        await notificationService.SendAsync(
            request.FromUserId, request.FromName, request.ToUserId, request.Message, cancellationToken);

        return Result.Success();
    }
}
