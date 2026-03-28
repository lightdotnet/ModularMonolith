using Monolith;
using Xunit;

namespace Shared.Tests;

public class EfCoreResultTests
{
    [Fact]
    public void From_ShouldReturnSuccess_WhenRowsAffectedIsGreaterThanZero()
    {
        // Arrange
        int rowsAffected = 1;

        // Act
        var result = EfCoreResult.From(rowsAffected);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void From_ShouldReturnError_WhenRowsAffectedIsZero()
    {
        // Arrange
        int rowsAffected = 0;

        // Act
        var result = EfCoreResult.From(rowsAffected);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("No rows were affected. Please check the operation.", result.Message);
    }
}