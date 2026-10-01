using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Modules.Notifications.Application.Notifications.Commands;
using StarterKit.Modules.Notifications.Application.Notifications.Queries;
using StarterKit.Shared;

namespace StarterKit.Modules.Notifications.Endpoints;

[ApiExplorerSettings(GroupName = "push")]
public class NotificationController(ICurrentUser currentUser) : VersionedApiController
{
    [HttpGet]
    [MustHavePermission(NotificationPermissions.Read)]
    public async Task<IActionResult> GetAsync([FromQuery] NotificationLookup request)
    {
        return Ok(await Mediator.Send(new SearchNotificationsQuery(request)));
    }

    [HttpPost]
    [MustHavePermission(NotificationPermissions.Send)]
    public async Task<IActionResult> SendToUserId(
        string toUserId,
        [FromBody] SystemMessage request)
    {
        // The sender comes from the authenticated token so a caller cannot spoof it.
        return Ok(await Mediator.Send(
            new SendNotificationCommand(
                toUserId,
                request,
                currentUser.UserId)));
    }

    [HttpPost("force_logout")]
    [MustHavePermission(NotificationPermissions.Send)]
    public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutMessage request)
    {
        return Ok(await Mediator.Send(new ForceLogoutCommand(request)));
    }
}
