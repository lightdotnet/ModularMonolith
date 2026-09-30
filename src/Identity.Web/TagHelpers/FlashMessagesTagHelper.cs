using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// <c>&lt;flash-messages /&gt;</c> renders (and consumes) the TempData success/error messages
/// set through <see cref="FlashMessages"/> as dismissible Bootstrap alerts.
/// </summary>
[HtmlTargetElement("flash-messages", TagStructure = TagStructure.WithoutEndTag)]
public sealed class FlashMessagesTagHelper(ITempDataDictionaryFactory tempDataFactory) : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var tempData = tempDataFactory.GetTempData(ViewContext.HttpContext);

        var content = new HtmlContentBuilder();

        AppendAlert(content, tempData[FlashMessages.SuccessKey] as string, "alert-success");
        AppendAlert(content, tempData[FlashMessages.ErrorKey] as string, "alert-danger");

        if (content.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(content);
    }

    private static void AppendAlert(HtmlContentBuilder content, string? message, string cssClass)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var alert = new TagBuilder("div");
        alert.AddCssClass($"alert {cssClass} alert-dismissible fade show");
        alert.Attributes["role"] = "alert";
        alert.InnerHtml.Append(message);

        var close = new TagBuilder("button");
        close.AddCssClass("btn-close");
        close.Attributes["type"] = "button";
        close.Attributes["data-bs-dismiss"] = "alert";
        close.Attributes["aria-label"] = "Close";
        alert.InnerHtml.AppendHtml(close);

        content.AppendHtml(alert);
    }
}
