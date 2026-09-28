using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Infrastructure;
using StarterKit.WebMvc.Models;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Services.Notifications;
using StarterKit.WebMvc.Web;
using StarterKit.WebMvc.Web.DataTables;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Controllers;

/// <summary>
/// The signed-in user's own notifications (<c>/notifications</c>): inbox, detail (marks read),
/// "mark all read", plus the JSON/partial endpoints behind the navbar bell and the SignalR token.
/// </summary>
[Route("notifications")]
public sealed class MyNotificationsController(
    IMyNotificationClient myNotificationClient,
    IAuthClient authClient,
    IOptions<SignalROptions> signalROptions)
    : WebControllerBase
{
    private const int BellItemCount = 8;

    [HttpGet("")]
    public async Task<IActionResult> Index(
        PagedQuery query,
        NotificationStatus? status,
        CancellationToken cancellationToken)
    {
        var sort = DataTableSortMaps.ForNotifications(query);

        var result = await myNotificationClient.SearchAsync(
            new NotificationLookup
            {
                PageNumber = query.Page,
                PageSize = query.PageSize,
                Status = status,
                SortBy = sort?.SortBy,
                SortDirection = sort?.SortDirection,
            },
            cancellationToken);

        var model = new MyNotificationsIndexViewModel(
            DataTableResult<NotificationDto>.FromApi(
                result,
                query),
            status);

        return ViewOrDataTable(
            "_InboxTable",
            model);
    }

    /// <summary>Shows one notification; the backend marks it read as part of this GET.</summary>
    [HttpGet("view/{id}")]
    public async Task<IActionResult> Detail(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await myNotificationClient.GetAsync(
            id,
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            // A success without data means the entry does not exist (for this user).
            Flash(
                FlashType.Error,
                result.IsSuccess || string.IsNullOrWhiteSpace(result.Message) ? "Notification not found." : result.Message);

            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    /// <summary>Partial with the latest notifications, loaded into the bell dropdown.</summary>
    [HttpGet("bell")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Bell(CancellationToken cancellationToken)
    {
        var result = await myNotificationClient.SearchAsync(
            new NotificationLookup
            {
                PageNumber = 1,
                PageSize = BellItemCount,
            },
            cancellationToken);

        return PartialView(
            "_BellItems",
            result.IsSuccess && result.Data is not null
                ? result.Data.Records.ToList()
                : null);
    }

    [HttpGet("unread-count")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var result = await myNotificationClient.CountUnreadAsync(cancellationToken);

        return result.IsSuccess
            ? Json(new { count = result.Data })
            : StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = result.Message });
    }

    /// <summary>
    /// Marks every unread notification of the user read in one backend call
    /// (<c>POST user_notification/read_all</c>). Fetch callers get <c>{ marked, message }</c>; form
    /// posts get a flash message and a redirect back to the list.
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        var result = await myNotificationClient.ReadAllAsync(cancellationToken);
        var marked = result.IsSuccess ? result.Data : 0;
        var message = result.IsSuccess
            ? MarkedText(marked)
            : result.Message;

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return result.IsSuccess
                ? Json(new { marked, message })
                : StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message });
        }

        Flash(
            result.IsSuccess ? FlashType.Success : FlashType.Error,
            message);

        return RedirectToLocal(
            returnUrl,
            nameof(Index));
    }

    /// <summary>
    /// Hands the browser what it needs to open the SignalR hub connection directly: a short-lived,
    /// hub-audience-only token minted per call by <c>POST auth/token/hub</c>, plus the hub URL from
    /// server-side config — the session JWT itself never leaves the httpOnly cookie. The client
    /// calls this on every (re)connect. Mirrors the admin client's <c>getSignalRTokenAction</c>;
    /// a near-expiry session token has already been refreshed by the cookie handler at this point.
    /// </summary>
    [HttpGet("hub-token")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> HubToken(CancellationToken cancellationToken)
    {
        var result = await authClient.GetHubTokenAsync(cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = result.Message });
        }

        return Json(new
        {
            accessToken = result.Data.AccessToken,
            expiresIn = result.Data.ExpiresIn,
            hubUrl = signalROptions.Value.HubUrl,
        });
    }

    private static string MarkedText(int count) =>
        count switch
        {
            <= 0 => "No unread notifications.",
            1 => "Marked 1 notification as read.",
            _ => $"Marked {count.ToString("#,##0", CultureInfo.InvariantCulture)} notifications as read.",
        };
}
