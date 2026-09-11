using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Notifications.Api.Application.Notifications.Commands;
using StarterKit.Notifications.Api.Application.Notifications.Queries;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.Shared;

namespace StarterKit.Notifications.Api.Controllers;

[ApiExplorerSettings(GroupName = "push")]
public class UserNotificationController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] NotificationLookup request)
    {
        request.ToUserId = _currentUserId;

        return Ok(await Mediator.Send(new SearchNotificationsQuery(request)));
    }

    [HttpGet("{entryId}")]
    public async Task<IActionResult> Get(string entryId)
    {
        await Mediator.Send(new MarkNotificationReadCommand(_currentUserId, entryId));

        return Ok(await Mediator.Send(new GetNotificationQuery(_currentUserId, entryId)));
    }

    [HttpGet("count_unread")]
    public async Task<IActionResult> CountUnread()
    {
        return Ok(await Mediator.Send(new CountUnreadNotificationsQuery(_currentUserId)));
    }
}
