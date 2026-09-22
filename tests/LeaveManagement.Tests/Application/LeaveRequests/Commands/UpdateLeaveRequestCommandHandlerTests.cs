using Light.Contracts;
using LeaveManagement.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
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

    private static readonly ILogger<UpdateLeaveRequestCommandHandler> Logger =
        NullLogger<UpdateLeaveRequestCommandHandler>.Instance;

    private static ApprovalStatusView StatusView(string approvalRequestId, string requestId, ApprovalStatus status) =>
        new(approvalRequestId, "LeaveRequest", requestId, status, 1);

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
            .Setup(s => s.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalStatusView?)null);
        return (orgServiceMock, approvalServiceMock);
    }

    private static UpdateLeaveRequestCommandHandler MakeHandler(
        LeaveManagementTestHost host,
        Mock<IOrgDirectoryService> orgServiceMock,
        Mock<IApprovalService> approvalServiceMock) =>
        new(
            host.Context,
            orgServiceMock.Object,
            new LeaveRequestApprovalCoordinator(host.Context, approvalServiceMock.Object, orgServiceMock.Object),
            approvalServiceMock.Object,
            Logger);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        // Arrange — Resubmit's own ConflictException guard is defense-in-depth here: the handler's
        // explicit IsOwnerActionable check below rejects this before Resubmit is ever reached.
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Approved);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);
        var model = ValidModel with { StartDate = ValidModel.EndDate.AddDays(1) };

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, model, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert — DateRange's ValidationException, translated by Guard into Result.Error, all the
        // way through the full handler (not just the unit-level DateRange test).
        Assert.False(result.IsSuccess);
        Assert.Equal("end: End date cannot be before start date.", result.Message);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenApproverMissingAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);
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
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);
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
    public async Task Handle_ShouldRejectResubmit_WhenOverlappingAnotherPendingRequestExists()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending,
            approvalRequestId: "old-approval");
        var blocking = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual, ValidModel.StartDate, ValidModel.EndDate,
            LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddRangeAsync(
            [entity, blocking], TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert — overlap is checked before the old workflow is cancelled or a new one created
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCancelAndResubmit_WhenValidNonManageEdit()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending,
            approvalRequestId: "old-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.CancelAsync("old-approval", "owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        Assert.Equal(ValidModel.StartDate, updated.Period.Start);
        Assert.Equal(ValidModel.EndDate, updated.Period.End);
    }

    [Fact]
    public async Task Handle_ShouldMutateMetadataOnly_WhenCanManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "someone-else", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Approved,
            approvalRequestId: "old-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);
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
    public async Task Handle_ShouldRejectManageEdit_WhenOverlappingAnotherRequestExists()
    {
        // Arrange — the manage-edit path is metadata-only but still enforces the overlap guard
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Approved,
            approvalRequestId: "old-approval");
        var blocking = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual, ValidModel.StartDate, ValidModel.EndDate,
            LeaveRequestStatus.Approved);
        await host.Context.LeaveRequests.AddRangeAsync(
            [entity, blocking], TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);
        var model = ValidModel with { ApproverEmployeeId = null };

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, model, "manager-user", true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveType.Annual, persisted.LeaveType);
    }

    [Fact]
    public async Task Handle_ShouldReconcileBeforeAuthorize_RejectingEditWhenApprovalNowApproved()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending,
            approvalRequestId: "old-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StatusView("old-approval", entity.Id, ApprovalStatus.Approved));
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending,
            approvalRequestId: "old-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.CancelAsync("old-approval", "owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("cannot withdraw"));
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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

    [Fact]
    public async Task Handle_ShouldCompensateByCancellingNewApproval_WhenFinalSaveFails()
    {
        // Arrange — force the final SaveChangesAsync (after Resubmit has already run in-memory) to
        // throw by disposing the shared Sqlite connection from inside the CreateAsync callback,
        // i.e. exactly between the new approval being created and the local commit. This exercises
        // the same best-effort-cancel compensation path already covered for CreateLeaveRequest.
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending,
            approvalRequestId: "old-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => host.Context.Database.GetDbConnection().Dispose())
            .ReturnsAsync(Result<string>.Success("approval-2"));
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(s => s.CancelAsync("approval-2", "owner", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCompensateAndTranslateException_WhenEntityBecomesNonActionableBeforeResubmit()
    {
        // Arrange — simulate the entity's status flipping to a terminal outcome (e.g. a concurrent
        // ApprovalFinalizedIntegrationEvent) in the window between the reconcile-before-authorize
        // check and Resubmit's own guard, via the CreateAsync callback: this is exactly the race
        // Resubmit's ConflictException guard exists to catch. Unlike
        // Handle_ShouldCompensateByCancellingNewApproval_WhenFinalSaveFails (a plain exception),
        // this exercises the `ex is ExceptionBase domainEx` branch specifically.
        using var host = new LeaveManagementTestHost();
        var entity = LeaveRequestBuilder.Build(
            "owner", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), LeaveRequestStatus.Pending,
            approvalRequestId: "old-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (orgServiceMock, approvalServiceMock) = CreateMocks();
        approvalServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => entity.ApplyApprovalOutcome("old-approval", LeaveRequestStatus.Approved))
            .ReturnsAsync(Result<string>.Success("approval-2"));
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new UpdateLeaveRequestCommand(entity.Id, ValidModel, "owner", false),
            TestContext.Current.CancellationToken);

        // Assert — Resubmit's ConflictException, translated by ToResult into a Result.Conflict
        Assert.False(result.IsSuccess);
        Assert.Equal("This leave request can no longer be resubmitted.", result.Message);
        approvalServiceMock.Verify(s => s.CancelAsync("approval-2", "owner", It.IsAny<CancellationToken>()), Times.Once);
    }
}
