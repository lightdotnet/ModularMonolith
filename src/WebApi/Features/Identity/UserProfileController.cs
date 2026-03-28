using Light.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Monolith.Identity;
using Monolith.Identity.Jwt;
using Monolith.Identity.Notifications;
using Monolith.Notifications;

namespace Monolith.Features.Identity;

[ApiExplorerSettings(GroupName = "")]
public class UserProfileController(
    ICurrentUser currentUser,
    IUserService userService,
    JwtTokenMananger jwtTokenMananger,
    INotificationService notificationService) : ApiControllerBase
{
    private readonly string _userId = currentUser.UserId ?? throw new UnauthorizedException();

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var res = await userService.GetByIdAsync(_userId);
        return Ok(res);
    }

    [HttpGet("token/list")]
    public async Task<IActionResult> GetTokens()
    {
        var res = await jwtTokenMananger.GetUserTokensAsync(_userId);
        return Ok(res);
    }

    [HttpPut("token/revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] string tokenId)
    {
        await jwtTokenMananger.RevokedAsync(_userId, tokenId);
        return Ok();
    }

    [HttpGet("notification")]
    public async Task<IActionResult> GetNotificationById([FromQuery] NotificationLookup request)
    {
        request.ToUserId = _userId;
        var res = await notificationService.GetAsync(request);
        return Ok(res);
    }

    [HttpGet("notification/{id}")]
    public async Task<IActionResult> GetNotifications(string id)
    {
        var res = await notificationService.GetByIdAsync(_userId, id);
        return Ok(res);
    }

    [HttpGet("notification/count_unread")]
    public async Task<IActionResult> CountUnreadNotifications()
    {
        var res = await notificationService.CountUnreadAsync(_userId);
        return Ok(res);
    }

    [HttpPut("notification/read")]
    public async Task<IActionResult> ReadNotificationById([FromBody] string id)
    {
        if (id == "all")
        {
            await notificationService.ReadAllAsync(_userId);
        }
        else
        {
            await notificationService.MarkAsReadAsync(_userId, id);
        }

        return Ok();
    }
}
