using StarterKit.Modules.Notifications.Persistence;

namespace StarterKit.Modules.Notifications.Features.Notifications.Commands;

internal sealed record MarkNotificationReadCommand(string UserId, string Id) : ICommand<IResult>;

internal class MarkNotificationReadCommandHandler(NotificationDbContext context)
    : ICommandHandler<MarkNotificationReadCommand, IResult>
{
    public async Task<IResult> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Notifications
            .SingleOrDefaultAsync(
                x => x.Id == request.Id && x.ToUserId == request.UserId,
                cancellationToken);

        // An unknown id, or one addressed to another user, is a no-op.
        if (entity is null)
            return Result.Success();

        entity.MarkAsRead();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
