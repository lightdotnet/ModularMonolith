using StarterKit.Shared;
using System.Text.Json;
using Xunit;

namespace Framework.Tests.Shared;

public class StatusTests
{
    [Fact]
    public void DefaultConstructor_ShouldDefaultToActive()
    {
        // Act
        var status = new ActiveStatus();

        // Assert
        Assert.Equal(ActiveStatus.State.Active, status.Value);
        Assert.True(status.IsActive);
    }

    [Theory]
    [InlineData(ActiveStatus.State.Inactive, false, true, false)]
    [InlineData(ActiveStatus.State.Active, true, false, false)]
    [InlineData(ActiveStatus.State.Locked, false, false, true)]
    public void Flags_ShouldReflectValue(ActiveStatus.State value, bool isActive, bool isInactive, bool isLocked)
    {
        // Act
        var status = new ActiveStatus(value);

        // Assert
        Assert.Equal(isActive, status.IsActive);
        Assert.Equal(isInactive, status.IsInactive);
        Assert.Equal(isLocked, status.IsLocked);
    }

    [Fact]
    public void Update_ShouldMutateValueAndFlags()
    {
        // Arrange
        var status = new ActiveStatus(ActiveStatus.State.Active);

        // Act
        status.Update(ActiveStatus.State.Locked);

        // Assert
        Assert.Equal(ActiveStatus.State.Locked, status.Value);
        Assert.True(status.IsLocked);
        Assert.False(status.IsActive);
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenValueIsTheSame()
    {
        // Arrange
        var a = new ActiveStatus(ActiveStatus.State.Locked);
        var b = new ActiveStatus(ActiveStatus.State.Locked);

        // Assert
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenValueDiffers()
    {
        // Arrange
        var a = new ActiveStatus(ActiveStatus.State.Active);
        var b = new ActiveStatus(ActiveStatus.State.Locked);

        // Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Serialize_ShouldOnlyIncludeValue()
    {
        // Arrange
        var status = new ActiveStatus(ActiveStatus.State.Active);

        // Act
        var json = JsonSerializer.Serialize(status);

        // Assert
        Assert.Contains("\"Value\"", json);
        Assert.DoesNotContain("IsActive", json);
        Assert.DoesNotContain("IsInactive", json);
        Assert.DoesNotContain("IsLocked", json);
    }
}
