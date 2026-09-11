using LeaveManagement.Tests.TestSupport;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Queries;

public class SearchLeaveRequestsQueryHandlerTests
{
    private static readonly DateTimeOffset StartDate = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndDate = new(2026, 5, 6, 0, 0, 0, TimeSpan.Zero);

    private static LeaveRequest MakeEntity(
        string userId,
        string employeeId,
        LeaveType leaveType,
        LeaveRequestStatus status,
        string? approvalRequestId = null) =>
        LeaveRequestBuilder.Build(
            userId, employeeId, leaveType, StartDate, EndDate, status,
            approvalRequestId: approvalRequestId);

    [Fact]
    public async Task Handle_ShouldScopeToOwnUser_WhenCannotManage()
    {
        // Arrange — self-service search now scopes by UserId, not EmployeeId.
        using var host = new LeaveManagementTestHost();
        await host.Context.LeaveRequests.AddRangeAsync(
            MakeEntity("user-1", "employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            MakeEntity("user-2", "employee-2", LeaveType.Annual, LeaveRequestStatus.Pending));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(
                new LeaveRequestSearchRequest(), "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        var record = Assert.Single(result.Data.Records);
        Assert.Equal("user-1", record.UserId);
    }

    [Fact]
    public async Task Handle_ShouldHonorEmployeeFilter_WhenCanManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await host.Context.LeaveRequests.AddRangeAsync(
            MakeEntity("user-1", "employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            MakeEntity("user-2", "employee-2", LeaveType.Annual, LeaveRequestStatus.Pending));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(
                new LeaveRequestSearchRequest { EmployeeId = "employee-2" }, "user-1", true),
            TestContext.Current.CancellationToken);

        // Assert
        var record = Assert.Single(result.Data.Records);
        Assert.Equal("employee-2", record.EmployeeId);
    }

    [Fact]
    public async Task Handle_ShouldFilterByLeaveType()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await host.Context.LeaveRequests.AddRangeAsync(
            MakeEntity("user-1", "employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            MakeEntity("user-1", "employee-1", LeaveType.Sick, LeaveRequestStatus.Pending));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(
                new LeaveRequestSearchRequest { LeaveType = LeaveType.Sick }, "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        var record = Assert.Single(result.Data.Records);
        Assert.Equal(LeaveType.Sick, record.LeaveType);
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await host.Context.LeaveRequests.AddRangeAsync(
            MakeEntity("user-1", "employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            MakeEntity("user-1", "employee-1", LeaveType.Annual, LeaveRequestStatus.Rejected));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(
                new LeaveRequestSearchRequest { Status = LeaveRequestStatus.Rejected }, "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        var record = Assert.Single(result.Data.Records);
        Assert.Equal(LeaveRequestStatus.Rejected, record.Status);
    }

    [Fact]
    public async Task Handle_ShouldProjectPeriod_IntoStartDateAndEndDate()
    {
        // Arrange — regression guard for the hand-written .Select(...) projection that replaced
        // Mapster's ProjectToType (which EF Core could not translate against the required Period
        // complex type). This runs the real query translation against the Sqlite-backed context,
        // not just an in-memory Adapt call, so it also catches a broken EF Core translation.
        using var host = new LeaveManagementTestHost();
        await host.Context.LeaveRequests.AddAsync(
            MakeEntity("user-1", "employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(new LeaveRequestSearchRequest(), "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        var record = Assert.Single(result.Data.Records);
        Assert.Equal(StartDate, record.StartDate);
        Assert.Equal(EndDate, record.EndDate);
    }
}
