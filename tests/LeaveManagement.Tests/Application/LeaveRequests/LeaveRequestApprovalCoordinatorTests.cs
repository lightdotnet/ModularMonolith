using LeaveManagement.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Organization.Contracts.Services;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests;

/// <summary>
/// Exercises <c>EnsureNoOverlapAsync</c> end-to-end against a real Sqlite-backed
/// <see cref="StarterKit.LeaveManagement.Api.Data.LeaveManagementDbContext"/>. See
/// <c>LeaveManagement.Tests.Domain.LeaveRequests.OverlappingLeaveRequestsSpecTests</c> for the
/// same overlap predicate exercised purely in-memory.
/// </summary>
public class LeaveRequestApprovalCoordinatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

    private static LeaveRequestApprovalCoordinator MakeCoordinator(LeaveManagementTestHost host) =>
        new(host.Context, new Mock<IApprovalService>().Object, new Mock<IOrgDirectoryService>().Object);

    private static async Task<LeaveRequest> SeedAsync(
        LeaveManagementTestHost host,
        LeaveRequestStatus status,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var entity = LeaveRequestBuilder.Build("user-1", "employee-1", LeaveType.Annual, start, end, status);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entity;
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Pending)]
    [InlineData(LeaveRequestStatus.Approved)]
    public async Task EnsureNoOverlapAsync_ShouldError_WhenBlockingStatusOverlaps(LeaveRequestStatus blockingStatus)
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await SeedAsync(host, blockingStatus, Start, End);
        var coordinator = MakeCoordinator(host);
        var candidate = new DateRange(Start.AddDays(2), End.AddDays(2));

        // Act
        var result = await coordinator.EnsureNoOverlapAsync(
            "employee-1", candidate, excludeRequestId: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public async Task EnsureNoOverlapAsync_ShouldSucceed_WhenNonBlockingStatusOverlaps(LeaveRequestStatus nonBlockingStatus)
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await SeedAsync(host, nonBlockingStatus, Start, End);
        var coordinator = MakeCoordinator(host);
        var candidate = new DateRange(Start.AddDays(2), End.AddDays(2));

        // Act
        var result = await coordinator.EnsureNoOverlapAsync(
            "employee-1", candidate, excludeRequestId: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureNoOverlapAsync_ShouldSucceed_WhenPeriodsDoNotOverlap()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await SeedAsync(host, LeaveRequestStatus.Approved, Start, End);
        var coordinator = MakeCoordinator(host);
        var candidate = new DateRange(End.AddDays(5), End.AddDays(10));

        // Act
        var result = await coordinator.EnsureNoOverlapAsync(
            "employee-1", candidate, excludeRequestId: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureNoOverlapAsync_ShouldError_WhenPeriodsTouchAtASingleBoundaryDay()
    {
        // Arrange — the overlap check is inclusive at both ends, exercised here against the real
        // Sqlite-backed DbContext (see OverlappingLeaveRequestsSpecTests for the in-memory version).
        using var host = new LeaveManagementTestHost();
        await SeedAsync(host, LeaveRequestStatus.Approved, Start, End);
        var coordinator = MakeCoordinator(host);
        var candidate = new DateRange(End, End.AddDays(5));

        // Act
        var result = await coordinator.EnsureNoOverlapAsync(
            "employee-1", candidate, excludeRequestId: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureNoOverlapAsync_ShouldSucceed_WhenPeriodStartsTheDayAfterExistingPeriodEnds()
    {
        // Arrange — one calendar day of separation is not an overlap
        using var host = new LeaveManagementTestHost();
        await SeedAsync(host, LeaveRequestStatus.Approved, Start, End);
        var coordinator = MakeCoordinator(host);
        var candidate = new DateRange(End.AddDays(1), End.AddDays(6));

        // Act
        var result = await coordinator.EnsureNoOverlapAsync(
            "employee-1", candidate, excludeRequestId: null, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureNoOverlapAsync_ShouldSucceed_WhenExcludingTheOverlappingRowItself()
    {
        // Arrange — editing/resubmitting a request against its own current period must not self-block
        using var host = new LeaveManagementTestHost();
        var entity = await SeedAsync(host, LeaveRequestStatus.Pending, Start, End);
        var coordinator = MakeCoordinator(host);

        // Act
        var result = await coordinator.EnsureNoOverlapAsync(
            "employee-1", new DateRange(Start, End), entity.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReconcileStatusAsync_ShouldLeaveRowUnchanged_WhenReturnedViewBelongsToASupersededWorkflow()
    {
        // Arrange — the row has since been resubmitted against a fresh workflow; a status view for
        // the old (superseded) approval id must not be applied. Mirrors the equivalent mismatched-id
        // guard already covered at the entity level (LeaveRequestTests) and the integration-event
        // subscriber level (ApprovalFinalizedIntegrationEventHandlerTests).
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, LeaveRequestStatus.Pending,
            approvalRequestId: "current-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusView("superseded-approval", "LeaveRequest", entity.Id, ApprovalStatus.Approved, 1));
        var coordinator = new LeaveRequestApprovalCoordinator(
            host.Context, approvalServiceMock.Object, new Mock<IOrgDirectoryService>().Object);

        // Act
        await coordinator.ReconcileStatusAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Pending, persisted.Status);
    }

    [Fact]
    public void Guard_ShouldReturnSuccess_WhenActionSucceeds()
    {
        // Act
        var result = LeaveRequestApprovalCoordinator.Guard(() => "value");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Data);
    }

    [Fact]
    public void Guard_ShouldReturnConflict_WhenConflictExceptionThrown()
    {
        // Act
        var result = LeaveRequestApprovalCoordinator.Guard<string>(
            () => throw new ConflictException("already linked"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("already linked", result.Message);
    }

    [Fact]
    public void Guard_ShouldReturnForbidden_WhenForbiddenExceptionThrown()
    {
        // Act
        var result = LeaveRequestApprovalCoordinator.Guard<string>(
            () => throw new ForbiddenException("not allowed"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("not allowed", result.Message);
    }

    [Fact]
    public void Guard_ShouldDescribeValidationErrors_WhenValidationExceptionThrown()
    {
        // Act
        var result = LeaveRequestApprovalCoordinator.Guard<string>(
            () => throw new ValidationException(new Dictionary<string, string[]> { ["end"] = ["End date cannot be before start date."] }));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("end: End date cannot be before start date.", result.Message);
    }

    [Fact]
    public void Guard_NoReturnValueOverload_ShouldReturnConflict_WhenConflictExceptionThrown()
    {
        // Act
        var result = LeaveRequestApprovalCoordinator.Guard(() => throw new ConflictException("cannot resubmit"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("cannot resubmit", result.Message);
    }
}
