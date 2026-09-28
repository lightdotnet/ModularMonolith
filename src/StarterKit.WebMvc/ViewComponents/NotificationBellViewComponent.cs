using Microsoft.AspNetCore.Mvc;
using StarterKit.WebMvc.Services.Notifications;

namespace StarterKit.WebMvc.ViewComponents;

/// <summary>
/// Navbar bell: the unread count is rendered server-side (one backend call per page); the dropdown
/// items are loaded lazily from <c>GET /notifications/bell</c> when opened, and
/// <c>notifications.js</c> keeps both live over the SignalR hub.
/// </summary>
public sealed class NotificationBellViewComponent(
    IMyNotificationClient myNotificationClient)
    : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await myNotificationClient.CountUnreadAsync(HttpContext.RequestAborted);

        // A failed count must not break every page — the bell then just shows no badge.
        return View(result.IsSuccess ? result.Data : (int?)null);
    }
}
