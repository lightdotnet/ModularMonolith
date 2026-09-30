using Identity.Tests.TestSupport;
using Moq;
using StarterKit.Modules.Identity.Web.TagHelpers;
using Xunit;

namespace Identity.Tests.Web.TagHelpers;

public class FeatureTagHelperTests
{
    [Theory]
    [InlineData(true, "a")]
    [InlineData(false, null)]
    public void Process_ShouldKeepElementOnlyWhenFeatureEnabled(bool enabled, string? expectedTag)
    {
        // Arrange
        var toggle = new Mock<IFeatureToggle>();
        toggle.Setup(t => t.IsEnabled("Admin.Users")).Returns(enabled);

        var sut = new FeatureTagHelper(toggle.Object) { Feature = "Admin.Users" };
        var output = TagHelperTestContext.Output("a");

        // Act
        sut.Process(TagHelperTestContext.Context(), output);

        // Assert
        Assert.Equal(expectedTag, output.TagName);
    }

    [Fact]
    public void Process_WhenNoFeatureGiven_ShouldSuppressElement()
    {
        // Arrange
        var sut = new FeatureTagHelper(Mock.Of<IFeatureToggle>()) { Feature = null };
        var output = TagHelperTestContext.Output("a");

        // Act
        sut.Process(TagHelperTestContext.Context(), output);

        // Assert
        Assert.Null(output.TagName);
    }
}
