using StarterKit.Modules.Notifications.Domain;
using System.Linq.Expressions;

namespace StarterKit.Modules.Notifications.Extensions;

internal static class DataMapper
{
    private static readonly Expression<Func<Notification, NotificationDto>> NotificationMapperExpression = x => new NotificationDto
    {
        Id = x.Id,
        FromUserId = x.FromUserId,
        FromName = x.FromName,
        ToUserId = x.ToUserId,
        Title = x.Title,
        Message = x.Message,
        Url = x.Url,
        Status = x.Status,
        Created = x.Created,
    };

    public static IQueryable<NotificationDto> MapToDto(this IQueryable<Notification> query) =>
        query.Select(NotificationMapperExpression);
}
