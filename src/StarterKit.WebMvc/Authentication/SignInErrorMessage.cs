using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Hands a sign-in error to the login page across a redirect (external-login callback, sign-in
/// rate limit) through TempData — Data-Protection-encrypted, so unlike a <c>?error=</c> query value
/// it cannot be forged into a link that shows attacker-chosen text on the login page.
/// </summary>
public static class SignInErrorMessage
{
    private const string TempDataKey = "__signin_error";

    public static void Set(
        ITempDataDictionary tempData,
        string message)
    {
        tempData[TempDataKey] = message;
    }

    /// <summary>Reads and consumes the pending message, if any.</summary>
    public static string? Take(ITempDataDictionary tempData) =>
        tempData[TempDataKey] as string;
}
