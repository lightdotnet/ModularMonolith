using StarterKit.Shared;
using Xunit;

namespace Framework.Tests.Shared;

public class AffectedRowsResultTests
{
    [Fact]
    public void From_ShouldReturnSuccess_WhenRowsAffectedIsGreaterThanZero()
    {
        // Arrange
        int rowsAffected = 1;

        // Act
        var result = AffectedRowsResult.From(rowsAffected);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void From_ShouldReturnError_WhenRowsAffectedIsZero()
    {
        // Arrange
        int rowsAffected = 0;

        // Act
        var result = AffectedRowsResult.From(rowsAffected);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No rows were affected. Please check the operation.", result.Message);
    }
}
