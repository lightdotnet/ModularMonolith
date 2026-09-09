using Light.Contracts;
using LeaveManagement.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using StarterKit.Organization.Contracts.Services;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Commands;

public class UpdateLeaveRequestCommandHandlerTests
{
    private static readonly UpdateLeaveRequest ValidModel = new(
        LeaveType.Sick,
        new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 2, 3, 0, 0, 0, TimeSpan.Zero),
        "Flu",
        "approver-1");

    private static readonly ResolvedApproverDto Approver = new()
    {
        EmployeeId = "approver-1",
        UserId = "approver-user-1",
        Name = "Alice Approver",
    };

    private static ApprovalStatusView StatusView(string requestId, ApprovalStatus status) =>
        new("approval-1", "LeaveRequest", requestId, status, 1);

    private static (Mock<IOrgDirectoryService>, Mock<IApprovalService>) CreateMocks()
    {
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Approver]);
        orgServiceMock
            .Setup(s => s.GetEmployeeNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Jane Requester");
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("approval-2"));
        // The handler checks CancelAsync's result, so an unstubbed mock (null) would NRE.
        approvalServiceMock
            .Setup(s => s.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        // Reconcile-before-authorize: nothing changed upstream by default.
        approvalServiceMock
            .Setup(s => s.GetStatusByRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalStatusView?)null);
        return (orgServiceMock, approvalServiceMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand("missing", ValidModel, "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNotOwnerAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Pending,
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "someone-else", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNotEditableStatusAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Approved,
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenEndDateBeforeStartDate()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Pending,
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);
        var model = ValidModel with { StartDate = ValidModel.EndDate.AddDays(1) };

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, model, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenApproverMissingAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Pending,
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);
        var model = ValidModel with { ApproverEmployeeId = null };

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, model, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenApproverInvalidAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Pending,
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);
        var model = ValidModel with { ApproverEmployeeId = "someone-else" };

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, model, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCancelAndResubmit_WhenValidNonManageEdit()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Pending,
            ApprovalRequestId = "old-approval",
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.CancelAsync("old-approval", "owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        approvalServiceMock.Verify(s => s.CancelAsync("old-approval", "owner", It.IsAny<CancellationToken>()), Times.Once);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        var updated = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal("approval-2", updated.ApprovalRequestId);
        Assert.Equal(LeaveRequestStatus.Pending, updated.Status);
        Assert.Equal(LeaveType.Sick, updated.LeaveType);
    }

    [Fact]
    public async Task Handle_ShouldMutateMetadataOnly_WhenCanManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "someone-else",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Approved,
            ApprovalRequestId = "old-approval",
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);
        var model = ValidModel with { ApproverEmployeeId = null };

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, model, "manager-user", true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        var updated = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Approved, updated.Status);
        Assert.Equal("old-approval", updated.ApprovalRequestId);
        Assert.Equal(LeaveType.Sick, updated.LeaveType);
    }

    [Fact]
    public async Task Handle_ShouldReconcileBeforeAuthorize_RejectingEditWhenApprovalNowApproved()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Pending,
            ApprovalRequestId = "old-approval",
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.GetStatusByRequestAsync("LeaveRequest", entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StatusView(entity.Id, ApprovalStatus.Approved));
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert — status is reconciled to Approved before the edit gate, so the edit is refused
        Assert.False(result.IsSuccess);
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Approved, persisted.Status);
        approvalServiceMock.Verify(
            s => s.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAbortResubmit_BeforeCreateAsync_WhenCancelFails()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = new LeaveRequest
        {
            UserId = "owner",
            EmployeeId = "employee-1",
            LeaveType = LeaveType.Annual,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Status = LeaveRequestStatus.Pending,
            ApprovalRequestId = "old-approval",
        };
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.CancelAsync("old-approval", "owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("cannot withdraw"));
        var handler = new UpdateLeaveRequestCommandHandler(host.Context, orgServiceMock.Object, approvalServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveType.Annual, persisted.LeaveType);
        Assert.Equal("old-approval", persisted.ApprovalRequestId);
    }
}
