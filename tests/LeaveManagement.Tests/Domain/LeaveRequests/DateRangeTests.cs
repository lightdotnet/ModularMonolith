using Light.Exceptions;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Domain.LeaveRequests;

public class DateRangeTests
{
    [Fact]
    public void Constructor_ShouldSucceed_WhenEndIsAfterStart()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

        // Act
        var period = new DateRange(start, end);

        // Assert
        Assert.Equal(start, period.Start);
        Assert.Equal(end, period.End);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenEndEqualsStart()
    {
        // Arrange
        var day = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);

        // Act
        var period = new DateRange(day, day);

        // Assert
        Assert.Equal(day, period.Start);
        Assert.Equal(day, period.End);
    }

    [Fact]
    public void Constructor_ShouldThrowValidationException_WhenEndIsBeforeStart()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(-1);

        // Act
        var ex = Assert.Throws<ValidationException>(() => new DateRange(start, end));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("end", error.Key);
        Assert.Equal("End date cannot be before start date.", Assert.Single(error.Value));
    }
}
