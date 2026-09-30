using StarterKit.Modules.Notifications.SignalR;
using Xunit;

namespace Notifications.Tests.SignalR;

public class NotificationHubOptionsSetupTests
{
    [Theory]
    [InlineData("/signalr-hub")]
    [InlineData("/hubs/notify")]
    [InlineData("/x")]
    [InlineData("/apis")]
    [InlineData("/hubs/api")]
    public void IsValidHubPath_ShouldReturnTrue_WhenPathIsValid(string path)
    {
        // Act
        var valid = NotificationHubOptionsSetup.IsValidHubPath(path);

        // Assert
        Assert.True(valid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathIsNullOrEmpty(string? path)
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath(path));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathIsWhitespace(string path)
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath(path));
    }

    [Theory]
    [InlineData("signalr-hub")]
    [InlineData("hubs/notify")]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathHasNoLeadingSlash(string path)
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath(path));
    }

    [Fact]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathIsBareSlash()
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath("/"));
    }

    [Theory]
    [InlineData("/signalr-hub/")]
    [InlineData("/hubs/notify/")]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathHasTrailingSlash(string path)
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath(path));
    }

    [Theory]
    [InlineData("/signalr hub")]
    [InlineData("/signalr\thub")]
    [InlineData(" /signalr-hub")]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathContainsWhitespace(string path)
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath(path));
    }

    [Theory]
    [InlineData("/signalr-hub?x=1")]
    [InlineData("/signalr-hub#frag")]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathContainsQueryOrFragment(string path)
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath(path));
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/API")]
    [InlineData("/api/x")]
    [InlineData("/api/v1/hub")]
    public void IsValidHubPath_ShouldReturnFalse_WhenPathOverlapsApiNamespace(string path)
    {
        // Act & Assert
        Assert.False(NotificationHubOptionsSetup.IsValidHubPath(path));
    }

    // The "hub path is a parent of /api" rule has no separately reachable input: the only
    // segment-wise parents of "/api" are "/" (rejected by the length rule, covered above) and
    // "/api" itself (covered by the overlap theory).
}
