using LeaveManagement.Tests.TestSupport;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Queries;

public class GetLeaveRequestByIdQueryHandlerTests
{
    private static LeaveRequest MakeEntity(
        string employeeId,
        LeaveRequestStatus status,
        string? approvalRequestId = null) => new()
    {
        UserId = "user-1",
        EmployeeId = employeeId,
        LeaveType = LeaveType.Annual,
        StartDate = DateTimeOffset.UtcNow,
        EndDate = DateTimeOffset.UtcNow.AddDays(1),
        Status = status,
        ApprovalRequestId = approvalRequestId,
    };

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery("missing", "employee-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNotOwnerAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("employee-1", LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(entity.Id, "employee-2", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnDto_WhenOwner()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("employee-1", LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(entity.Id, "employee-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(entity.Id, result.Data.Id);
    }
}
