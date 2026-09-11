using Light.Contracts;
using LeaveManagement.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using StarterKit.Organization.Contracts.Services;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Commands;

public class DeleteLeaveRequestCommandHandlerTests
{
    private static readonly ILogger<DeleteLeaveRequestCommandHandler> Logger =
        NullLogger<DeleteLeaveRequestCommandHandler>.Instance;

    private static LeaveRequest MakeEntity(
        string userId,
        LeaveRequestStatus status,
        string? approvalRequestId = null) =>
        LeaveRequestBuilder.Build(
            userId, "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), status,
            approvalRequestId: approvalRequestId);

    private static ApprovalStatusView StatusView(string requestId, ApprovalStatus status) =>
        new("approval-1", "LeaveRequest", requestId, status, 1);

    private static DeleteLeaveRequestCommandHandler MakeHandler(
        LeaveManagementTestHost host,
        Mock<IApprovalService> approvalServiceMock)
    {
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        return new DeleteLeaveRequestCommandHandler(
            host.Context,
            new LeaveRequestApprovalCoordinator(host.Context, approvalServiceMock.Object, orgServiceMock.Object),
            approvalServiceMock.Object,
            Logger);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = MakeHandler(host, approvalServiceMock);

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
        var handler = MakeHandler(host, approvalServiceMock);

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
        var handler = MakeHandler(host, approvalServiceMock);

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
        var handler = MakeHandler(host, approvalServiceMock);

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
        var handler = MakeHandler(host, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "manager-user", true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(host.Context.LeaveRequests);
    }

    [Fact]
    public async Task Handle_ShouldNotDelete_WhenNonManageAndApprovalCancelFails()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("owner", LeaveRequestStatus.Pending, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.CancelAsync("approval-1", "owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("cannot cancel"));
        var handler = MakeHandler(host, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(host.Context.LeaveRequests);
    }

    [Fact]
    public async Task Handle_ShouldDeleteAnyway_WhenManageAndApprovalCancelFails()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("owner", LeaveRequestStatus.Pending, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.CancelAsync("approval-1", "owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("cannot cancel"));
        var handler = MakeHandler(host, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "manager-user", true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(host.Context.LeaveRequests);
    }

    [Fact]
    public async Task Handle_ShouldReconcileBeforeAuthorize_BlockingNonManageDeleteOfNowApprovedRequest()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("owner", LeaveRequestStatus.Pending, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.GetStatusByRequestAsync("LeaveRequest", entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StatusView(entity.Id, ApprovalStatus.Approved));
        var handler = MakeHandler(host, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new DeleteLeaveRequestCommand(entity.Id, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert — reconciled to Approved before the status gate, so a non-manage delete is refused
        Assert.False(result.IsSuccess);
        var persisted = Assert.Single(host.Context.LeaveRequests);
        Assert.Equal(LeaveRequestStatus.Approved, persisted.Status);
        approvalServiceMock.Verify(
            s => s.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
