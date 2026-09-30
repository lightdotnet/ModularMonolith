using Identity.Tests.TestSupport;
using Microsoft.AspNetCore.Authorization;
using Moq;
using StarterKit.Modules.Identity.Web.TagHelpers;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Web.TagHelpers;

public class PermissionTagHelperTests
{
    private static Mock<IAuthorizationService> CreateAuthorization(params string[] grantedPolicies)
    {
        var mock = new Mock<IAuthorizationService>();

        mock
            .Setup(s => s.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        foreach (var policy in grantedPolicies)
        {
            mock
                .Setup(s => s.AuthorizeAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<object?>(),
                    policy))
                .ReturnsAsync(AuthorizationResult.Success());
        }

        return mock;
    }

    private static PermissionTagHelper CreateSut(Mock<IAuthorizationService> authorization, string permission) =>
        new(authorization.Object)
        {
            Permission = permission,
            ViewContext = TagHelperTestContext.ViewContext(),
        };

    [Fact]
    public async Task ProcessAsync_WhenAuthorized_ShouldKeepElement()
    {
        // Arrange
        var sut = CreateSut(CreateAuthorization("users.view"), "users.view");
        var output = TagHelperTestContext.Output("a");

        // Act
        await sut.ProcessAsync(TagHelperTestContext.Context(), output);

        // Assert
        Assert.Equal("a", output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_WhenNotAuthorized_ShouldSuppressElement()
    {
        // Arrange
        var sut = CreateSut(CreateAuthorization(), "users.view");
        var output = TagHelperTestContext.Output("a");

        // Act
        await sut.ProcessAsync(TagHelperTestContext.Context(), output);

        // Assert
        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_WhenAnyOfSeveralPoliciesAuthorizes_ShouldKeepElement()
    {
        // Arrange
        var sut = CreateSut(CreateAuthorization("roles.view"), "users.view, roles.view");
        var output = TagHelperTestContext.Output("a");

        // Act
        await sut.ProcessAsync(TagHelperTestContext.Context(), output);

        // Assert
        Assert.Equal("a", output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_WhenNoPolicyGiven_ShouldSuppressElement()
    {
        // Arrange
        var authorization = CreateAuthorization();
        var sut = CreateSut(authorization, " ");
        var output = TagHelperTestContext.Output("a");

        // Act
        await sut.ProcessAsync(TagHelperTestContext.Context(), output);

        // Assert
        Assert.Null(output.TagName);
        authorization.VerifyNoOtherCalls();
    }
}
