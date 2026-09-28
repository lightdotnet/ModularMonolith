using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StarterKit.WebMvc.Pages;

/// <summary>
/// Target of both the exception handler (unhandled errors) and status-code pages (404, ...).
/// </summary>
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErrorModel : PageModel
{
    public int? ErrorStatusCode { get; private set; }

    public string? RequestId { get; private set; }

    public string Title => ErrorStatusCode switch
    {
        StatusCodes.Status404NotFound => "Page not found",
        StatusCodes.Status403Forbidden => "Access denied",
        _ => "Something went wrong",
    };

    public string Description => ErrorStatusCode switch
    {
        StatusCodes.Status404NotFound => "The page you are looking for does not exist.",
        StatusCodes.Status403Forbidden => "You do not have permission to view this page.",
        _ => "An error occurred while processing your request.",
    };

    public void OnGet(int? statusCode) => Load(statusCode);

    // The exception handler / status-code pages re-execute with the original request's method.
    public void OnPost(int? statusCode) => Load(statusCode);

    private void Load(int? statusCode)
    {
        ErrorStatusCode = statusCode;
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
