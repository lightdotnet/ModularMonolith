using StarterKit.WebMvc.Authentication;
using Xunit;

namespace StarterKit.WebMvc.Tests.Authentication;

public class SafeReturnUrlTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/users")]
    [InlineData("/users/42?tab=roles#top")]
    [InlineData("/a//b")]
    public void Sanitize_ShouldKeepLocalPaths(string returnUrl)
    {
        // Act
        var result = SafeReturnUrl.Sanitize(returnUrl);

        // Assert
        Assert.Equal(returnUrl, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("https://evil.example/")]
    [InlineData("http://localhost/users")]
    [InlineData("javascript:alert(1)")]
    [InlineData("users")]
    [InlineData("\\\\evil.example")]
    public void Sanitize_ShouldFallBackToDefault_ForUnsafeValues(string? returnUrl)
    {
        // Act
        var result = SafeReturnUrl.Sanitize(returnUrl);

        // Assert
        Assert.Equal(SafeReturnUrl.Default, result);
    }
}
