using StarterKit.Modules.Notifications.Application.Common;

namespace StarterKit.Modules.Notifications.Application.Notifications.Commands;

internal sealed record MarkNotificationReadCommand(string UserId, string Id) : ICommand<IResult>;

internal class MarkNotificationReadCommandHandler(INotificationDbContext context)
    : ICommandHandler<MarkNotificationReadCommand, IResult>
{
    public async Task<IResult> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Notifications
            .SingleOrDefaultAsync(
                x => x.Id == request.Id && x.RecipientUserId == request.UserId,
                cancellationToken);

        // An unknown id, or one addressed to another user, is a no-op.
        if (entity is null)
            return Result.Success();

        entity.MarkAsRead();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
