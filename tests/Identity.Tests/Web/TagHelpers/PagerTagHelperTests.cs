using Identity.Tests.TestSupport;
using Light.Contracts;
using Microsoft.AspNetCore.Http;
using StarterKit.Modules.Identity.Web.TagHelpers;
using System.Net;
using Xunit;

namespace Identity.Tests.Web.TagHelpers;

public class PagerTagHelperTests
{
    private static HttpContext CreateHttpContext(string path, string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Request.QueryString = new QueryString(queryString);
        return httpContext;
    }

    private static string Render(IPaged model, HttpContext httpContext)
    {
        var sut = new PagerTagHelper
        {
            Model = model,
            ViewContext = TagHelperTestContext.ViewContext(httpContext),
        };
        var output = TagHelperTestContext.Output("pager");

        sut.Process(TagHelperTestContext.Context(), output);

        Assert.Equal("nav", output.TagName);
        return WebUtility.HtmlDecode(output.Content.GetContent());
    }

    [Fact]
    public void Process_ShouldLinkPagesAndPreserveQueryString()
    {
        // Arrange
        var model = new Paged<string>(["x"], pageNumber: 2, pageSize: 10, totalRecords: 45);
        var httpContext = CreateHttpContext("/Admin/Users", "?SearchValue=abc&PageNumber=2&PageSize=10");

        // Act
        var html = Render(model, httpContext);

        // Assert
        Assert.Contains("href=\"/Admin/Users?SearchValue=abc&PageSize=10&PageNumber=1\"", html);
        Assert.Contains("href=\"/Admin/Users?SearchValue=abc&PageSize=10&PageNumber=3\"", html);
        Assert.Contains("href=\"/Admin/Users?SearchValue=abc&PageSize=10&PageNumber=5\"", html);
        Assert.DoesNotContain("PageNumber=2\"", html);
        Assert.Contains("aria-current=\"page\"", html);
        Assert.Contains("Showing 11–20 of 45", html);
    }

    [Fact]
    public void Process_OnFirstPage_ShouldDisablePrevious()
    {
        // Arrange
        var model = new Paged<string>(["x"], pageNumber: 1, pageSize: 10, totalRecords: 25);
        var httpContext = CreateHttpContext("/Admin/Users", string.Empty);

        // Act
        var html = Render(model, httpContext);

        // Assert
        Assert.DoesNotContain("<a aria-label=\"Previous\"", html);
        Assert.DoesNotContain("PageNumber=0", html);
        Assert.DoesNotContain("PageNumber=1\"", html);
        Assert.Contains("href=\"/Admin/Users?PageNumber=2\"", html);
    }

    [Fact]
    public void Process_WhenSinglePage_ShouldRenderSummaryOnly()
    {
        // Arrange
        var model = new Paged<string>(["x"], pageNumber: 1, pageSize: 10, totalRecords: 3);

        // Act
        var html = Render(model, CreateHttpContext("/Admin/Users", string.Empty));

        // Assert
        Assert.Contains("Showing 1–3 of 3", html);
        Assert.DoesNotContain("pagination", html);
    }

    [Fact]
    public void Process_WhenEmpty_ShouldSuppressOutput()
    {
        // Arrange
        var sut = new PagerTagHelper
        {
            Model = new Paged<string>([], pageNumber: 1, pageSize: 10, totalRecords: 0),
            ViewContext = TagHelperTestContext.ViewContext(),
        };
        var output = TagHelperTestContext.Output("pager");

        // Act
        sut.Process(TagHelperTestContext.Context(), output);

        // Assert
        Assert.Null(output.TagName);
    }

    [Fact]
    public void PageNumbers_ShouldKeepEdgesAndMarkGaps()
    {
        // Act
        var pages = PagerTagHelper.PageNumbers(current: 6, totalPages: 12, window: 2).ToArray();

        // Assert
        Assert.Equal([1, null, 4, 5, 6, 7, 8, null, 12], pages);
    }
}
