using StarterKit.Notifications.Contracts.Services;

namespace StarterKit.Notifications.Api.Application.Notifications.Commands;

/// <summary>
/// Marks every unread notification of the given user as read; returns the number of entries changed.
/// </summary>
internal sealed record ReadAllNotificationsCommand(string UserId) : ICommand<IResult<int>>;

internal class ReadAllNotificationsCommandHandler(INotificationService notificationService)
    : ICommandHandler<ReadAllNotificationsCommand, IResult<int>>
{
    public async Task<IResult<int>> Handle(
        ReadAllNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        var count = await notificationService.ReadAllAsync(request.UserId);

        return Result<int>.Success(count);
    }
}
