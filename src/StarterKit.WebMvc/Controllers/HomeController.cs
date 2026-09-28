using Microsoft.AspNetCore.Mvc;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Models;
using StarterKit.WebMvc.Services.Notifications;

namespace StarterKit.WebMvc.Controllers;

public sealed class HomeController(
    IMyNotificationClient myNotificationClient)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var unread = await myNotificationClient.CountUnreadAsync(cancellationToken);

        var model = new HomeIndexViewModel(
            User.GetDisplayName(),
            User.GetUserName(),
            User.GetRoles(),
            User.GetPermissions().Count,
            unread.IsSuccess ? unread.Data : null);

        return View(model);
    }
}
