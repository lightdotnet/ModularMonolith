using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// TempData keys of the one-shot status messages rendered by <c>&lt;flash-messages /&gt;</c>.
/// </summary>
public static class FlashMessages
{
    public const string SuccessKey = "Flash.Success";

    public const string ErrorKey = "Flash.Error";

    public static void SetSuccess(this ITempDataDictionary tempData, string message) =>
        tempData[SuccessKey] = message;

    public static void SetError(this ITempDataDictionary tempData, string message) =>
        tempData[ErrorKey] = message;
}
