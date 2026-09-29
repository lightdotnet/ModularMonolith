using StarterKit.Infrastructure.Services;
using StarterKit.Shared;
using Xunit;

namespace Framework.Tests.Infrastructure.Services;

public class DateTimeServiceTests
{
    [Fact]
    public void UtcNow_ShouldBeCloseToRealUtcNow()
    {
        // Arrange
        IDateTime service = new DateTimeService();

        // Act
        var result = service.UtcNow;

        // Assert
        Assert.True((DateTimeOffset.UtcNow - result).Duration() < TimeSpan.FromSeconds(1));
    }
}
