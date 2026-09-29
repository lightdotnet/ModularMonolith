using Framework.Tests.Persistence.TestSupport;
using StarterKit.Persistence.Extensions;
using Xunit;

namespace Framework.Tests.Persistence.Extensions;

public class QueryableResultExtensionsTests
{
    private static async Task<TestDbContext> SeedAsync(int count)
    {
        var context = TestDbContext.CreateInMemory();
        for (var i = 0; i < count; i++)
        {
            context.Add(new TestAggregate());
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return context;
    }

    [Fact]
    public async Task ToPagedAsync_ShouldTreatNonPositivePageNumber_AsOne()
    {
        // Arrange
        using var context = await SeedAsync(5);

        // Act
        var paged = await context.Aggregates.OrderBy(a => a.Id)
            .ToPagedAsync(pageNumber: 0, pageSize: 2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, paged.PageNumber);
        Assert.Equal(2, paged.Records.Count());
        Assert.Equal(5, paged.TotalRecords);
    }

    [Fact]
    public async Task ToPagedAsync_ShouldTreatNonPositivePageSize_AsTen()
    {
        // Arrange
        using var context = await SeedAsync(5);

        // Act
        var paged = await context.Aggregates.OrderBy(a => a.Id)
            .ToPagedAsync(pageNumber: 1, pageSize: 0, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, paged.Records.Count());
        Assert.Equal(5, paged.TotalRecords);
    }

    [Fact]
    public async Task ToPagedAsync_ShouldReturnEmptyItems_WhenPagingPastTheEnd()
    {
        // Arrange
        using var context = await SeedAsync(3);

        // Act
        var paged = await context.Aggregates.OrderBy(a => a.Id)
            .ToPagedAsync(pageNumber: 5, pageSize: 2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(paged.Records);
        Assert.Equal(3, paged.TotalRecords);
    }

    [Fact]
    public async Task FirstResultAsync_ShouldReturnNotFound_WhenQueryableIsEmpty()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();

        // Act
        var result = await context.Aggregates.FirstResultAsync("TestAggregate", "n/a", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Query object TestAggregate by n/a not found", result.Message);
    }

    [Fact]
    public async Task LastResultAsync_ShouldReturnNotFound_WhenQueryableIsEmpty()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();

        // Act
        var result = await context.Aggregates.LastResultAsync("TestAggregate", "n/a", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Query object TestAggregate by n/a not found", result.Message);
    }

    [Fact]
    public async Task SingleResultAsync_ShouldReturnNotFound_WhenQueryableIsEmpty()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();

        // Act
        var result = await context.Aggregates.SingleResultAsync("TestAggregate", "n/a", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Query object TestAggregate by n/a not found", result.Message);
    }

    [Fact]
    public async Task SingleResultAsync_ShouldThrow_WhenMoreThanOneMatch()
    {
        // Arrange
        using var context = await SeedAsync(2);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Aggregates.SingleResultAsync("TestAggregate", "any", TestContext.Current.CancellationToken));
    }
}
