using Light.Contracts;
using LeaveManagement.Tests.TestSupport;
using Moq;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Commands;

public class DeleteLeaveRequestCommandHandlerTests
{
    private static LeaveRequest MakeEntity(
        string userId,
        LeaveRequestStatus status,
        string? approvalRequestId = null) => new()
    {
        UserId = userId,
        EmployeeId = "employee-1",
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
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new DeleteLeaveRequestCommandHandler(host.Context, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand("missing", "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNotOwnerAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("owner", LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new DeleteLeaveRequestCommandHandler(host.Context, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "someone-else", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(host.Context.LeaveRequests);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNotDeletableStatusAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("owner", LeaveRequestStatus.Approved);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new DeleteLeaveRequestCommandHandler(host.Context, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldCancelApproval_WhenPendingWithApprovalRequestId()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("owner", LeaveRequestStatus.Pending, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.CancelAsync("approval-1", "owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var handler = new DeleteLeaveRequestCommandHandler(host.Context, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        approvalServiceMock.Verify(s => s.CancelAsync("approval-1", "owner", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(host.Context.LeaveRequests);
    }

    [Fact]
    public async Task Handle_ShouldDelete_WhenCanManageRegardlessOfOwnerAndStatus()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("someone-else", LeaveRequestStatus.Approved);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new DeleteLeaveRequestCommandHandler(host.Context, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "manager-user", true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(host.Context.LeaveRequests);
    }
}
