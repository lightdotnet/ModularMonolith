using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Modules.Notifications.Features.Notifications.Commands;
using StarterKit.Modules.Notifications.Features.Notifications.Queries;

namespace StarterKit.Modules.Notifications.Endpoints;

[ApiExplorerSettings(GroupName = "push")]
public class NotificationController : VersionedApiController
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
        string fromUserId,
        string? fromName,
        string toUserId,
        [FromBody] SystemMessage request)
    {
        return Ok(await Mediator.Send(new SendNotificationCommand(
            fromUserId,
            fromName,
            toUserId,
            request)));
    }

    [HttpPost("force_logout")]
    [MustHavePermission(NotificationPermissions.Send)]
    public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutMessage request)
    {
        return Ok(await Mediator.Send(new ForceLogoutCommand(request)));
    }
}
