using StarterKit.Notifications.Contracts.Services;

namespace StarterKit.Notifications.Api.Application.Notifications.Commands;

internal sealed record MarkNotificationReadCommand(string UserId, string Id) : ICommand<IResult>;

internal class MarkNotificationReadCommandHandler(INotificationService notificationService)
    : ICommandHandler<MarkNotificationReadCommand, IResult>
{
    public async Task<IResult> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(request.UserId, request.Id);

        return Result.Success();
    }
}
