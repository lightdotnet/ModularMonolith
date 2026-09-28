using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StarterKit.WebMvc.Services.Http;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Web;

/// <summary>Shared body of <c>HandleApiResult</c> for <see cref="WebPageModel"/> and <see cref="WebControllerBase"/>.</summary>
internal static class WebResultHandler
{
    public static bool Handle(
        ApiResult result,
        string successMessage,
        ModelStateDictionary modelState,
        ITempDataDictionary tempData,
        string? prefix)
    {
        if (result.IsSuccess)
        {
            tempData.AddFlash(
                FlashType.Success,
                successMessage);

            return true;
        }

        modelState.AddApiErrors(
            result,
            prefix);

        return false;
    }
}
