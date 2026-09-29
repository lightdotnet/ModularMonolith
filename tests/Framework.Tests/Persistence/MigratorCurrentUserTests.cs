using StarterKit.Persistence.MigrationSupport;
using Xunit;

namespace Framework.Tests.Persistence;

public class MigratorCurrentUserTests
{
    [Fact]
    public void Defaults_ShouldRepresentAnUnauthenticatedSystemIdentity()
    {
        // Arrange
        var user = new MigratorCurrentUser();

        // Assert
        Assert.Equal("Migrator", user.UserId);
        Assert.Null(user.Username);
        Assert.Null(user.FirstName);
        Assert.Null(user.LastName);
        Assert.True(string.IsNullOrWhiteSpace(user.FullName));
        Assert.Null(user.PhoneNumber);
        Assert.Null(user.Email);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void HasPermissionAndIsInRole_ShouldReturnFalse_NotThrow()
    {
        // Arrange
        var user = new MigratorCurrentUser();

        // Act & Assert
        Assert.False(user.HasPermission("anything"));
        Assert.False(user.IsInRole("anything"));
    }
}
