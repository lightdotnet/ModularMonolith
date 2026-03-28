using Monolith.Authorization;
using Xunit;

namespace Shared.Tests.Authorization;

public class PermissionsTests
{
    [Fact]
    public void System_View_ShouldHaveCorrectValue()
    {
        // Assert
        Assert.Equal("System.View", Permissions.System.View);
    }

    [Fact]
    public void System_Notification_ShouldHaveCorrectValue()
    {
        // Assert
        Assert.Equal("System.Notification", Permissions.System.Notification);
    }

    [Fact]
    public void System_Manager_ShouldHaveCorrectValue()
    {
        // Assert
        Assert.Equal("System.Manager", Permissions.System.Manager);
    }
}