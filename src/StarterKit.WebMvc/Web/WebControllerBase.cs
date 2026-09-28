using Microsoft.AspNetCore.Mvc;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Http;
using StarterKit.WebMvc.Web.DataTables;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Web;

/// <summary>
/// Base controller for MVC screens — the controller counterpart of <see cref="WebPageModel"/>.
/// </summary>
public abstract class WebControllerBase : Controller
{
    private IPermissionChecker? _permissionChecker;

    protected IPermissionChecker PermissionChecker =>
        _permissionChecker ??= HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();

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

    /// <inheritdoc cref="WebPageModel"/>
    protected bool HandleApiResult(
        ApiResult result,
        string successMessage,
        string? prefix = null)
    {
        return WebResultHandler.Handle(
            result,
            successMessage,
            ModelState,
            TempData,
            prefix);
    }

    protected bool FlashApiResult(
        ApiResult result,
        string successMessage)
    {
        Flash(
            result.IsSuccess ? FlashType.Success : FlashType.Error,
            result.IsSuccess ? successMessage : result.Message);

        return result.IsSuccess;
    }

    protected IActionResult RedirectToLocal(
        string? returnUrl,
        string fallbackAction)
    {
        var safe = SafeReturnUrl.Sanitize(returnUrl);

        return safe == SafeReturnUrl.Default
            ? RedirectToAction(fallbackAction)
            : LocalRedirect(safe);
    }

    /// <summary>Returns the table partial for a data-table fragment request, else the full view.</summary>
    protected IActionResult ViewOrDataTable(
        string tablePartialName,
        object model)
    {
        return Request.IsDataTableFragment()
            ? PartialView(
                tablePartialName,
                model)
            : View(model);
    }
}
