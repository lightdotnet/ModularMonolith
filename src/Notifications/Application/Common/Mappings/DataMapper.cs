using StarterKit.Modules.Notifications.Domain;
using System.Linq.Expressions;

namespace StarterKit.Modules.Notifications.Application.Common.Mappings;

internal static class DataMapper
{
    private static readonly Expression<Func<Notification, NotificationDto>> NotificationMapperExpression = x => new NotificationDto
    {
        Id = x.Id,
        ToUserId = x.RecipientUserId,
        Title = x.Title,
        Message = x.Message,
        Url = x.Url,
        Status = x.Status,
        Created = x.Created,
        SenderUserId = x.SenderUserId,
        SenderName = x.SenderName,
    };

    public static IQueryable<NotificationDto> MapToDto(this IQueryable<Notification> query) =>
        query.Select(NotificationMapperExpression);
}
