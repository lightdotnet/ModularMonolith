using Microsoft.AspNetCore.Identity;
using StarterKit.Modules.Identity.Application.Common.Extensions;
using Xunit;

namespace Identity.Tests.Application.Common.Extensions;

public class IdentityResultExtensionsTests
{
    [Fact]
    public void ToResult_ShouldReturnSuccess_WhenIdentityResultSucceeded()
    {
        // Arrange
        var identityResult = IdentityResult.Success;

        // Act
        var result = identityResult.ToResult();

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ToResult_ShouldJoinErrorDescriptions_WhenIdentityResultFailed()
    {
        // Arrange
        var identityResult = IdentityResult.Failed(
            new IdentityError { Description = "Username already taken" },
            new IdentityError { Description = "Password too weak" });

        // Act
        var result = identityResult.ToResult();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Username already taken|Password too weak", result.Message);
    }

    [Fact]
    public void ToResultOfT_ShouldReturnSuccessWithData_WhenIdentityResultSucceeded()
    {
        // Arrange
        var identityResult = IdentityResult.Success;

        // Act
        var result = identityResult.ToResult("user-1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("user-1", result.Data);
    }

    [Fact]
    public void ToResultOfT_ShouldJoinErrorDescriptions_WhenIdentityResultFailed()
    {
        // Arrange
        var identityResult = IdentityResult.Failed(new IdentityError { Description = "Duplicate email" });

        // Act
        var result = identityResult.ToResult("user-1");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate email", result.Message);
    }
}
