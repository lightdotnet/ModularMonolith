using System.ComponentModel.DataAnnotations;
using StarterKit.Orders.Api.Application.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders;

public class OrphanedStockReconciliationOptionsTests
{
    private static List<ValidationResult> Validate(OrphanedStockReconciliationOptions options)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);

        return results;
    }

    [Fact]
    public void Defaults_ShouldBeValid()
    {
        // Arrange
        var options = new OrphanedStockReconciliationOptions();

        // Act
        var results = Validate(options);

        // Assert
        Assert.Empty(results);
        Assert.True(options.Enabled);
        Assert.Equal(5, options.IntervalMinutes);
        Assert.Equal(5, options.MinAgeMinutes);
        Assert.Equal(200, options.BatchSize);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(60)]
    public void MinAgeMinutes_ShouldBeValid_WhenAtLeastTwo(int minutes)
    {
        Assert.Empty(Validate(new OrphanedStockReconciliationOptions { MinAgeMinutes = minutes }));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-5)]
    public void MinAgeMinutes_ShouldFailValidation_WhenLessThanTwo(int minutes)
    {
        var results = Validate(new OrphanedStockReconciliationOptions { MinAgeMinutes = minutes });

        var failure = Assert.Single(results);
        Assert.Contains(nameof(OrphanedStockReconciliationOptions.MinAgeMinutes), failure.MemberNames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IntervalMinutes_ShouldFailValidation_WhenLessThanOne(int minutes)
    {
        var results = Validate(new OrphanedStockReconciliationOptions { IntervalMinutes = minutes });

        var failure = Assert.Single(results);
        Assert.Contains(nameof(OrphanedStockReconciliationOptions.IntervalMinutes), failure.MemberNames);
    }

    [Fact]
    public void IntervalMinutes_ShouldBeValid_WhenOne()
    {
        Assert.Empty(Validate(new OrphanedStockReconciliationOptions { IntervalMinutes = 1 }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BatchSize_ShouldFailValidation_WhenLessThanOne(int batchSize)
    {
        var results = Validate(new OrphanedStockReconciliationOptions { BatchSize = batchSize });

        var failure = Assert.Single(results);
        Assert.Contains(nameof(OrphanedStockReconciliationOptions.BatchSize), failure.MemberNames);
    }

    [Fact]
    public void BatchSize_ShouldBeValid_WhenOne()
    {
        Assert.Empty(Validate(new OrphanedStockReconciliationOptions { BatchSize = 1 }));
    }
}
