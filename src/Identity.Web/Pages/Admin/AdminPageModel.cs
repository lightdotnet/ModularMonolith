using IResult = Light.Contracts.IResult;
using Light.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StarterKit.Modules.Identity.Web.TagHelpers;
using StarterKit.Shared;
using StarterKit.Shared.Authorization;
using System.Reflection;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Modules.Identity.Web.Pages.Admin;

/// <summary>
/// Base for the admin pages: mediator access, handler-level permission checks, and
/// Post/Redirect/Get handling of a command's <see cref="IResult"/>.
/// </summary>
public abstract class AdminPageModel : PageModel
{
    public const string DefaultErrorMessage = "The operation could not be completed.";

    /// <summary>
    /// Model-state prefix of the form model the pages bind (<c>[Bind(Prefix = "Input")]</c>).
    /// </summary>
    public const string DefaultInputPrefix = "Input";

    private const string CommandModelPrefix = "Model.";

    private IMediator? _mediator;

    private ICurrentUser? _currentUser;

    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected ICurrentUser CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    /// <summary>
    /// Whether the signed-in user has full control (bypasses the permission-grant rules).
    /// </summary>
    public bool IsFullControl => CurrentUser.IsFullControl();

    /// <summary>
    /// Whether the signed-in user may grant or revoke <paramref name="permission"/>: only a
    /// permission they hold (or any, with full control). Mirrors the server-side guard; the UI
    /// uses it to disable what the guard would reject.
    /// </summary>
    public bool CanGrant(string permission) =>
        IsFullControl || CurrentUser.HasPermission(permission);

    /// <summary>
    /// Whether <paramref name="userId"/> is the signed-in user.
    /// </summary>
    public bool IsSelf(string? userId) =>
        !string.IsNullOrEmpty(userId)
        && string.Equals(CurrentUser.UserId, userId, StringComparison.Ordinal);

    /// <summary>
    /// Whether the current user satisfies <paramref name="policy"/> (a permission name).
    /// Use it in write handlers: the page-level <c>[Authorize]</c> only covers viewing.
    /// </summary>
    protected async Task<bool> IsAuthorizedAsync(string policy)
    {
        var authorizationService = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(User, policy);
        return result.Succeeded;
    }

    /// <summary>
    /// Sends <paramref name="request"/> through the mediator. When the validation pipeline
    /// rejects it, adds each error to <see cref="PageModel.ModelState"/> and returns <c>null</c>,
    /// so the caller re-renders the form: <c>if (result is null) return Page();</c>
    /// </summary>
    /// <param name="inputPrefix">
    /// Model-state prefix of the form model the errors map to: a command property
    /// <c>Model.Name</c> becomes <c>{inputPrefix}.Name</c> when the page's form model has that
    /// property. Empty maps onto the page's own properties.
    /// </param>
    /// <param name="unmappedKey">
    /// Model-state key for errors that do not map to a form field; empty (model-level) by default.
    /// </param>
    protected async Task<TResponse?> SendAsync<TResponse>(
        IRequest<TResponse> request,
        string inputPrefix = DefaultInputPrefix,
        string unmappedKey = "")
        where TResponse : class
    {
        try
        {
            return await Mediator.Send(request, HttpContext.RequestAborted);
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(
                exception.ValidationErrors,
                inputPrefix,
                unmappedKey);

            return null;
        }
    }

    /// <summary>
    /// Form handlers. On success sets the success flash message and redirects
    /// (<paramref name="redirect"/>, or the current page by default). On failure adds the
    /// result's errors to <see cref="PageModel.ModelState"/> and returns <c>null</c> so the
    /// caller re-renders the form: <c>return HandleResult(result, "Saved.") ?? await RenderAsync();</c>
    /// </summary>
    /// <param name="errorKey">
    /// Model-state key for the errors; empty (model-level) by default. Set it when a page has
    /// several forms, so the errors show next to the form that failed.
    /// </param>
    protected IActionResult? HandleResult(
        IResult result,
        string successMessage,
        Func<IActionResult>? redirect = null,
        string errorKey = "")
    {
        if (result.IsSuccess)
        {
            TempData.SetSuccess(successMessage);
            return redirect?.Invoke() ?? RedirectToPage();
        }

        AddResultErrors(result, errorKey);
        return null;
    }

    /// <summary>
    /// Row actions (e.g. delete from a list). Sets the success or error flash message and
    /// always redirects (<paramref name="redirect"/>, or the current page by default). A
    /// <c>null</c> <paramref name="result"/> (a request rejected by validation, see
    /// <see cref="SendAsync{TResponse}"/>) flashes the errors already in
    /// <see cref="PageModel.ModelState"/>.
    /// </summary>
    protected IActionResult RedirectWithResult(
        IResult? result,
        string successMessage,
        Func<IActionResult>? redirect = null)
    {
        if (result is null)
            TempData.SetError(string.Join(" ", ModelStateErrors()));
        else if (result.IsSuccess)
            TempData.SetSuccess(successMessage);
        else
            TempData.SetError(string.Join(" ", ErrorsOf(result)));

        return redirect?.Invoke() ?? RedirectToPage();
    }

    /// <summary>
    /// Adds the result's errors as model-level errors (Identity joins several errors with '|').
    /// </summary>
    protected void AddResultErrors(IResult result, string key = "")
    {
        foreach (var error in ErrorsOf(result))
            ModelState.AddModelError(key, error);
    }

    private void AddValidationErrors(
        IDictionary<string, string[]> errors,
        string inputPrefix,
        string unmappedKey)
    {
        var inputType = string.IsNullOrEmpty(inputPrefix)
            ? GetType()
            : GetType().GetProperty(inputPrefix, BindingFlags.Public | BindingFlags.Instance)?.PropertyType;

        foreach (var (propertyName, messages) in errors)
        {
            var key = MapValidationKey(
                propertyName,
                inputType,
                inputPrefix)
                ?? unmappedKey;

            foreach (var message in messages)
                ModelState.AddModelError(key, message);
        }
    }

    /// <summary>
    /// Maps a validator property name (<c>Model.Name</c>, <c>Model.Roles[0]</c>) onto the form
    /// model; <c>null</c> when the form model has no such property.
    /// </summary>
    private static string? MapValidationKey(
        string propertyName,
        Type? inputType,
        string inputPrefix)
    {
        if (inputType is null || string.IsNullOrEmpty(propertyName))
            return null;

        var name = propertyName.StartsWith(CommandModelPrefix, StringComparison.Ordinal)
            ? propertyName[CommandModelPrefix.Length..]
            : propertyName;

        var type = inputType;

        foreach (var segment in name.Split('.'))
        {
            var indexer = segment.IndexOf('[', StringComparison.Ordinal);
            var memberName = indexer < 0 ? segment : segment[..indexer];

            var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
                return null;

            type = property.PropertyType;

            if (indexer >= 0)
            {
                type = type.IsArray
                    ? type.GetElementType()!
                    : type.GetGenericArguments().FirstOrDefault() ?? type;
            }
        }

        return string.IsNullOrEmpty(inputPrefix) ? name : $"{inputPrefix}.{name}";
    }

    private List<string> ModelStateErrors()
    {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrEmpty(m))
            .ToList();

        return errors.Count == 0 ? [DefaultErrorMessage] : errors;
    }

    private static IEnumerable<string> ErrorsOf(IResult result)
    {
        var errors = (result.Message ?? string.Empty)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return errors.Length == 0 ? [DefaultErrorMessage] : errors;
    }
}
