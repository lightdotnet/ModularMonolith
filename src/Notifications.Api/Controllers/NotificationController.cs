using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Notifications.Api.Application.Notifications.Commands;
using StarterKit.Notifications.Api.Application.Notifications.Queries;
using StarterKit.Notifications.Contracts.Authorization;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.Controllers;

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
        await Mediator.Send(new SendNotificationCommand(fromUserId, fromName, toUserId, request));

        return Ok();
    }

    [HttpPost("force_logout")]
    [MustHavePermission(NotificationPermissions.Send)]
    public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutMessage request)
    {
        await Mediator.Send(new ForceLogoutCommand(request));

        return Ok();
    }
}
