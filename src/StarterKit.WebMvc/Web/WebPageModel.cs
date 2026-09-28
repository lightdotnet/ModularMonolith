using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Http;
using StarterKit.WebMvc.Web.DataTables;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Web;

/// <summary>
/// Base PageModel for screens: flash messages, API-result handling, permission checks for page
/// handlers (Razor Pages ignore authorization attributes on handler methods) and data-table
/// fragment responses.
/// </summary>
public abstract class WebPageModel : PageModel
{
    private IPermissionChecker? _permissionChecker;

    protected IPermissionChecker PermissionChecker =>
        _permissionChecker ??= HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();

    /// <summary>Queues a toast shown after the next redirect.</summary>
    protected void Flash(
        FlashType type,
        string message)
    {
        TempData.AddFlash(
            type,
            message);
    }

    protected bool Can(string permission) =>
        PermissionChecker.HasPermission(
            User,
            permission);

    /// <summary>
    /// On success queues <paramref name="successMessage"/> and returns true; on failure pushes the
    /// backend's field errors (under <paramref name="prefix"/>) or message into ModelState.
    /// </summary>
    protected bool HandleApiResult(
        ApiResult result,
        string successMessage,
        string? prefix = "Input")
    {
        return WebResultHandler.Handle(
            result,
            successMessage,
            ModelState,
            TempData,
            prefix);
    }

    /// <summary>
    /// Like <see cref="HandleApiResult"/> for actions without a form (delete, …): failures become an
    /// error toast instead of ModelState errors.
    /// </summary>
    protected bool FlashApiResult(
        ApiResult result,
        string successMessage)
    {
        Flash(
            result.IsSuccess ? FlashType.Success : FlashType.Error,
            result.IsSuccess ? successMessage : result.Message);

        return result.IsSuccess;
    }

    /// <summary>Redirects to a local <paramref name="returnUrl"/> (the list state the action was taken from), else to <paramref name="fallbackPage"/>.</summary>
    protected IActionResult RedirectToLocal(
        string? returnUrl,
        string fallbackPage)
    {
        var safe = SafeReturnUrl.Sanitize(returnUrl);

        return safe == SafeReturnUrl.Default
            ? RedirectToPage(fallbackPage)
            : LocalRedirect(safe);
    }

    /// <summary>Returns the table partial for a data-table fragment request, else the full page.</summary>
    protected IActionResult PageOrDataTable(string tablePartialName) =>
        Request.IsDataTableFragment()
            ? Partial(
                tablePartialName,
                this)
            : Page();
}
