using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Entities;

public class UserTests
{
    [Fact]
    public void UpdateInfo_ShouldSetFields()
    {
        // Arrange
        var user = new User { UserName = "jane.doe" };

        // Act
        user.UpdateInfo("Jane", "Doe", "555-0100", "jane@example.com");

        // Assert
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("555-0100", user.PhoneNumber);
        Assert.Equal("jane@example.com", user.Email);
    }

    [Theory]
    [InlineData(ActiveStatus.State.Active)]
    [InlineData(ActiveStatus.State.Locked)]
    public void UpdateStatus_ShouldApply_ForActiveOrLocked(ActiveStatus.State status)
    {
        // Arrange
        var user = new User { UserName = "jane.doe" };

        // Act
        user.UpdateStatus(status);

        // Assert
        Assert.Equal(status, user.Status.Value);
    }

    [Fact]
    public void UpdateStatus_ShouldBeNoOp_ForInactive()
    {
        // Arrange
        var user = new User { UserName = "jane.doe" };
        user.UpdateStatus(ActiveStatus.State.Locked);

        // Act
        user.UpdateStatus(ActiveStatus.State.Inactive);

        // Assert: Inactive is not one of the two statuses UpdateStatus allows, so it's ignored.
        Assert.Equal(ActiveStatus.State.Locked, user.Status.Value);
    }

    [Fact]
    public void ChangeAuthProvider_ShouldSetProvider_WhenExternal()
    {
        // Arrange
        var user = new User { UserName = "jane.doe" };

        // Act
        user.ChangeAuthProvider(AuthProvider.ActiveDirectory);

        // Assert
        Assert.Equal(AuthProvider.ActiveDirectory, user.AuthProvider);
    }

    [Fact]
    public void ChangeAuthProvider_ShouldResetToLocal_WhenLocal()
    {
        // Arrange
        var user = new User { UserName = "jane.doe", AuthProvider = AuthProvider.ActiveDirectory };

        // Act
        user.ChangeAuthProvider(AuthProvider.Local);

        // Assert
        Assert.Equal(AuthProvider.Local, user.AuthProvider);
    }

    [Fact]
    public void Delete_ShouldScrubPersonalData_AndLockStatus()
    {
        // Arrange
        var user = new User
        {
            UserName = "jane.doe",
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "555-0100",
            Email = "jane@example.com",
            PasswordHash = "hash",
            AuthProvider = AuthProvider.ActiveDirectory,
        };

        // Act
        user.Delete();

        // Assert
        Assert.Null(user.UserName);
        Assert.Null(user.FirstName);
        Assert.Null(user.LastName);
        Assert.Null(user.PhoneNumber);
        Assert.Null(user.Email);
        Assert.Null(user.PasswordHash);
        Assert.Equal(AuthProvider.Local, user.AuthProvider);
        Assert.Equal(ActiveStatus.State.Locked, user.Status.Value);
    }
}
