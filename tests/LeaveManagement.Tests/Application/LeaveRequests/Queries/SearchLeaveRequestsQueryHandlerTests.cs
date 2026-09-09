using LeaveManagement.Tests.TestSupport;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Queries;

public class SearchLeaveRequestsQueryHandlerTests
{
    private static LeaveRequest MakeEntity(
        string employeeId,
        LeaveType leaveType,
        LeaveRequestStatus status,
        string? approvalRequestId = null) => new()
    {
        UserId = "user-" + employeeId,
        EmployeeId = employeeId,
        LeaveType = leaveType,
        StartDate = DateTimeOffset.UtcNow,
        EndDate = DateTimeOffset.UtcNow.AddDays(1),
        Status = status,
        ApprovalRequestId = approvalRequestId,
    };

    [Fact]
    public async Task Handle_ShouldScopeToOwnEmployee_WhenCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await host.Context.LeaveRequests.AddRangeAsync(
            MakeEntity("employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            MakeEntity("employee-2", LeaveType.Annual, LeaveRequestStatus.Pending));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(
                new LeaveRequestSearchRequest { EmployeeId = "employee-2" }, "employee-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        var record = Assert.Single(result.Data.Records);
        Assert.Equal("employee-1", record.EmployeeId);
    }

    [Fact]
    public async Task Handle_ShouldHonorEmployeeFilter_WhenCanManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await host.Context.LeaveRequests.AddRangeAsync(
            MakeEntity("employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            MakeEntity("employee-2", LeaveType.Annual, LeaveRequestStatus.Pending));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(
                new LeaveRequestSearchRequest { EmployeeId = "employee-2" }, "employee-1", true),
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
            MakeEntity("employee-1", LeaveType.Annual, LeaveRequestStatus.Pending),
            MakeEntity("employee-1", LeaveType.Sick, LeaveRequestStatus.Pending));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchLeaveRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchLeaveRequestsQuery(
                new LeaveRequestSearchRequest { LeaveType = LeaveType.Sick }, "employee-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        var record = Assert.Single(result.Data.Records);
        Assert.Equal(LeaveType.Sick, record.LeaveType);
    }
}
