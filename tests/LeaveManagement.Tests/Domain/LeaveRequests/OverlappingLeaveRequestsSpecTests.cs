using LeaveManagement.Tests.TestSupport;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Domain.LeaveRequests;

/// <summary>
/// Exercises <see cref="OverlappingLeaveRequestsSpec"/> purely in-memory via
/// <c>Specification&lt;T&gt;.IsSatisfiedBy</c> (LINQ-to-Objects), independent of how <c>Period</c> is
/// mapped by EF Core. The same predicate is also exercised against a real Sqlite-backed
/// <c>LeaveManagementDbContext</c> (translated to SQL) in
/// <c>LeaveManagement.Tests.Application.LeaveRequests.LeaveRequestApprovalCoordinatorTests</c>.
/// </summary>
public class OverlappingLeaveRequestsSpecTests
{
    private static readonly DateTimeOffset Start = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 6, 5, 0, 0, 0, TimeSpan.Zero);

    private static LeaveRequest MakeExisting(LeaveRequestStatus status, DateTimeOffset start, DateTimeOffset end) =>
        LeaveRequestBuilder.Build("user-1", "employee-1", LeaveType.Annual, start, end, status);

    [Theory]
    [InlineData(LeaveRequestStatus.Pending)]
    [InlineData(LeaveRequestStatus.Approved)]
    public void IsSatisfiedBy_ShouldBeTrue_WhenBlockingStatusAndOverlappingPeriod(LeaveRequestStatus blockingStatus)
    {
        // Arrange
        var existing = MakeExisting(blockingStatus, Start, End);
        var spec = new OverlappingLeaveRequestsSpec("employee-1", new DateRange(Start.AddDays(2), End.AddDays(2)));

        // Act & Assert
        Assert.True(spec.IsSatisfiedBy(existing));
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public void IsSatisfiedBy_ShouldBeFalse_WhenNonBlockingStatusEvenIfOverlapping(LeaveRequestStatus nonBlockingStatus)
    {
        // Arrange
        var existing = MakeExisting(nonBlockingStatus, Start, End);
        var spec = new OverlappingLeaveRequestsSpec("employee-1", new DateRange(Start.AddDays(2), End.AddDays(2)));

        // Act & Assert
        Assert.False(spec.IsSatisfiedBy(existing));
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeFalse_WhenPeriodsDoNotOverlap()
    {
        // Arrange
        var existing = MakeExisting(LeaveRequestStatus.Approved, Start, End);
        var spec = new OverlappingLeaveRequestsSpec("employee-1", new DateRange(End.AddDays(5), End.AddDays(10)));

        // Act & Assert
        Assert.False(spec.IsSatisfiedBy(existing));
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeFalse_WhenExcludingTheOverlappingRowItself()
    {
        // Arrange — editing/resubmitting a request against its own current period must not self-block
        var existing = MakeExisting(LeaveRequestStatus.Pending, Start, End);
        var spec = new OverlappingLeaveRequestsSpec("employee-1", new DateRange(Start, End), existing.Id);

        // Act & Assert
        Assert.False(spec.IsSatisfiedBy(existing));
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeFalse_WhenDifferentEmployee()
    {
        // Arrange
        var existing = LeaveRequestBuilder.Build(
            "user-2", "employee-2", LeaveType.Annual, Start, End, LeaveRequestStatus.Approved);
        var spec = new OverlappingLeaveRequestsSpec("employee-1", new DateRange(Start, End));

        // Act & Assert
        Assert.False(spec.IsSatisfiedBy(existing));
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeTrue_WhenPeriodsTouchAtASingleBoundaryDay()
    {
        // Arrange — the spec's overlap check is inclusive at both ends
        var existing = MakeExisting(LeaveRequestStatus.Approved, Start, End);
        var spec = new OverlappingLeaveRequestsSpec("employee-1", new DateRange(End, End.AddDays(5)));

        // Act & Assert
        Assert.True(spec.IsSatisfiedBy(existing));
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeFalse_WhenPeriodStartsTheDayAfterExistingPeriodEnds()
    {
        // Arrange — one calendar day of separation is not an overlap
        var existing = MakeExisting(LeaveRequestStatus.Approved, Start, End);
        var spec = new OverlappingLeaveRequestsSpec("employee-1", new DateRange(End.AddDays(1), End.AddDays(6)));

        // Act & Assert
        Assert.False(spec.IsSatisfiedBy(existing));
    }
}
