using Identity.Tests.TestSupport;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.IntegrationEvents;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.IntegrationEvents;

public class UserIntegrationEventsTests
{
    [Fact]
    public void NextVersion_ShouldBeClockTimeInUnixMilliseconds()
    {
        // Arrange
        var clock = new FakeDateTime { UtcNow = new DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero) };

        // Act
        var version = UserIntegrationEvents.NextVersion(clock);

        // Assert
        Assert.Equal(clock.UtcNow.ToUnixTimeMilliseconds(), version);
    }

    [Fact]
    public void Provisioned_ShouldMapUserState_SourceAndVersion()
    {
        // Arrange
        var user = new User
        {
            UserName = "jane.doe",
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
        };

        // Act
        var evt = UserIntegrationEvents.Provisioned(user, ProvisioningSource.ExternalLogin, 42);

        // Assert
        Assert.Equal(user.Id, evt.UserId);
        Assert.Equal("jane@example.com", evt.Email);
        Assert.Equal("jane.doe", evt.UserName);
        Assert.Equal("Jane", evt.FirstName);
        Assert.Equal("Doe", evt.LastName);
        Assert.Equal(ProvisioningSource.ExternalLogin, evt.Source);
        Assert.Equal(42, evt.Version);
    }

    [Fact]
    public void Provisioned_ShouldMapMissingEmailAndUserNameToEmptyStrings()
    {
        // Arrange
        var user = new User();

        // Act
        var evt = UserIntegrationEvents.Provisioned(user, ProvisioningSource.Admin, 1);

        // Assert
        Assert.Equal(string.Empty, evt.Email);
        Assert.Equal(string.Empty, evt.UserName);
        Assert.Null(evt.FirstName);
        Assert.Null(evt.LastName);
    }

    [Fact]
    public void ProfileUpdated_ShouldMapProfileAndVersion()
    {
        // Arrange
        var user = new User { Email = "jane@example.com", FirstName = "Jane", LastName = "Doe" };

        // Act
        var evt = UserIntegrationEvents.ProfileUpdated(user, 7);

        // Assert
        Assert.Equal(user.Id, evt.UserId);
        Assert.Equal("jane@example.com", evt.Email);
        Assert.Equal("Jane", evt.FirstName);
        Assert.Equal("Doe", evt.LastName);
        Assert.Equal(7, evt.Version);
    }

    [Fact]
    public void ProfileUpdated_ShouldMapMissingEmailToEmptyString()
    {
        // Act
        var evt = UserIntegrationEvents.ProfileUpdated(new User(), 1);

        // Assert
        Assert.Equal(string.Empty, evt.Email);
    }

    [Theory]
    [InlineData(ActiveStatus.State.Active, true)]
    [InlineData(ActiveStatus.State.Locked, false)]
    [InlineData(ActiveStatus.State.Inactive, false)]
    public void StatusChanged_ShouldMapIsActiveFromStatus(ActiveStatus.State status, bool expectedIsActive)
    {
        // Arrange
        var user = new User { Status = new ActiveStatus(status) };

        // Act
        var evt = UserIntegrationEvents.StatusChanged(user, 3);

        // Assert
        Assert.Equal(user.Id, evt.UserId);
        Assert.Equal(expectedIsActive, evt.IsActive);
        Assert.Equal(3, evt.Version);
    }

    [Fact]
    public void Deleted_ShouldMapUserIdAndVersion()
    {
        // Act
        var evt = UserIntegrationEvents.Deleted("user-1", 9);

        // Assert
        Assert.Equal("user-1", evt.UserId);
        Assert.Equal(9, evt.Version);
    }
}
