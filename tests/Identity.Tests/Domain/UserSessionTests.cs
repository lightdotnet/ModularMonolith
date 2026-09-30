using StarterKit.Modules.Identity.Domain;
using Xunit;

namespace Identity.Tests.Domain;

public class UserSessionTests
{
    [Fact]
    public void GetTokenExpiresInSeconds_ShouldReturnPositiveValue_ForFutureExpiry()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession { UserId = "user-1", TokenExpiresAt = now.AddMinutes(10) };

        // Act
        var seconds = session.GetTokenExpiresInSeconds(now);

        // Assert
        Assert.InRange(seconds, 590, 600);
    }

    [Fact]
    public void GetTokenExpiresInSeconds_ShouldReturnNegativeValue_ForPastExpiry()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession { UserId = "user-1", TokenExpiresAt = now.AddMinutes(-10) };

        // Act
        var seconds = session.GetTokenExpiresInSeconds(now);

        // Assert
        Assert.True(seconds < 0);
    }
}
