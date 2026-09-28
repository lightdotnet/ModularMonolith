using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts;
using StarterKit.Notifications.Contracts.Authorization;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Models;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Services.Notifications;
using StarterKit.WebMvc.Web;
using StarterKit.WebMvc.Web.DataTables;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Controllers;

/// <summary>
/// Notification administration (<c>/notifications/admin</c>): every user's notifications, sending a
/// notification and forcing a user's sessions to sign out.
/// </summary>
[Route("notifications/admin")]
public sealed class NotificationsAdminController(
    INotificationAdminClient notificationAdminClient,
    IUserClient userClient)
    : WebControllerBase
{
    private const int UserLookupMinLength = 3;

    private const int UserLookupPageSize = 10;

    [HttpGet("")]
    [HasPermission(NotificationPermissions.Read)]
    public async Task<IActionResult> Index(
        PagedQuery query,
        NotificationStatus? status,
        string? toUserId,
        CancellationToken cancellationToken)
    {
        toUserId = string.IsNullOrWhiteSpace(toUserId) ? null : toUserId.Trim();
        var sort = DataTableSortMaps.ForNotifications(query);

        var result = await notificationAdminClient.SearchAsync(
            new NotificationLookup
            {
                PageNumber = query.Page,
                PageSize = query.PageSize,
                Status = status,
                ToUserId = toUserId,
                SortBy = sort?.SortBy,
                SortDirection = sort?.SortDirection,
            },
            cancellationToken);

        var model = new NotificationsAdminIndexViewModel(
            DataTableResult<NotificationDto>.FromApi(
                result,
                query),
            status,
            toUserId);

        return ViewOrDataTable(
            "_NotificationsTable",
            model);
    }

    [HttpGet("send")]
    [HasPermission(NotificationPermissions.Send)]
    public IActionResult Send(string? toUserId)
    {
        return View(new SendNotificationInput
        {
            ToUserId = toUserId ?? string.Empty,
        });
    }

    [HttpPost("send")]
    [HasPermission(NotificationPermissions.Send)]
    public async Task<IActionResult> Send(
        SendNotificationInput input,
        CancellationToken cancellationToken)
    {
        // [Required] on Message puts a missing body into ModelState; the null check also narrows the type.
        if (!ModelState.IsValid || input.Message is null)
        {
            input.Message ??= new SystemMessage();
            return View(input);
        }

        input.Message.ByMessage = false;

        var result = await notificationAdminClient.SendAsync(
            User.GetUserId() ?? string.Empty,
            User.GetDisplayName(),
            input.ToUserId.Trim(),
            input.Message,
            cancellationToken);

        if (!HandleApiResult(
            result,
            "Notification sent."))
        {
            return View(input);
        }

        return RedirectToAction(nameof(Send));
    }

    /// <summary>Pushes a force-logout message to every open session of the user.</summary>
    [HttpPost("force-logout")]
    [HasPermission(NotificationPermissions.Send)]
    public async Task<IActionResult> ForceLogout(
        string? userId,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            Flash(
                FlashType.Error,
                "Enter the user ID to sign out.");
        }
        else
        {
            var result = await notificationAdminClient.ForceLogoutAsync(
                new ForceLogoutMessage(userId.Trim()),
                cancellationToken);

            FlashApiResult(
                result,
                "Force logout sent.");
        }

        return RedirectToLocal(
            returnUrl,
            nameof(Send));
    }

    /// <summary>
    /// Small user search backing the recipient/filter inputs (JSON). Uses the Identity user search,
    /// so the backend additionally requires <c>identity.users.view</c> — without it this returns 403
    /// and the inputs still accept a typed user ID.
    /// </summary>
    [HttpGet("user-lookup")]
    public async Task<IActionResult> UserLookup(
        string? q,
        CancellationToken cancellationToken)
    {
        if (!Can(NotificationPermissions.Read) && !Can(NotificationPermissions.Send))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < UserLookupMinLength)
        {
            return Json(Array.Empty<UserLookupItem>());
        }

        var result = await userClient.SearchAsync(
            new SearchUserRequest
            {
                SearchValue = q.Trim(),
                PageNumber = 1,
                PageSize = UserLookupPageSize,
            },
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = result.Message });
        }

        return Json(result.Data.Records
            .Select(user => new UserLookupItem(
                user.Id,
                user.UserName,
                string.Join(' ', new[] { user.FirstName, user.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)))))
            .ToList());
    }
}
