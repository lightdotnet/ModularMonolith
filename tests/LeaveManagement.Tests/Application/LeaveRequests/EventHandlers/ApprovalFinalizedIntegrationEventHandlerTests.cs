using LeaveManagement.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.EventHandlers;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.EventHandlers;

public class ApprovalFinalizedIntegrationEventHandlerTests
{
    private static readonly ILogger<ApprovalFinalizedIntegrationEventHandler> Logger =
        NullLogger<ApprovalFinalizedIntegrationEventHandler>.Instance;

    private static LeaveRequest MakeEntity(
        LeaveRequestStatus status,
        string? approvalRequestId) =>
        LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), status,
            approvalRequestId: approvalRequestId);

    [Theory]
    [InlineData(ApprovalStatus.Approved, LeaveRequestStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected, LeaveRequestStatus.Rejected)]
    public async Task Handle_ShouldReconcileMatchingPendingRow_ToTerminalStatus(
        ApprovalStatus finalized,
        LeaveRequestStatus expected)
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity(LeaveRequestStatus.Pending, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ApprovalFinalizedIntegrationEventHandler(host.Context, Logger);

        // Act
        await handler.Handle(
            new ApprovalFinalizedIntegrationEvent("LeaveRequest", entity.Id, "approval-1", finalized),
            TestContext.Current.CancellationToken);

        // Assert
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(expected, persisted.Status);
    }

    [Fact]
    public async Task Handle_ShouldIgnore_WhenRequestTypeIsNotLeaveRequest()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity(LeaveRequestStatus.Pending, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ApprovalFinalizedIntegrationEventHandler(host.Context, Logger);

        // Act
        await handler.Handle(
            new ApprovalFinalizedIntegrationEvent("Expense", entity.Id, "approval-1", ApprovalStatus.Approved),
            TestContext.Current.CancellationToken);

        // Assert
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task Handle_ShouldIgnore_WhenApprovalRequestIdDoesNotMatchTheRow()
    {
        // Arrange — the row has since been resubmitted against a fresh workflow
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity(LeaveRequestStatus.Pending, "current-approval");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ApprovalFinalizedIntegrationEventHandler(host.Context, Logger);

        // Act
        await handler.Handle(
            new ApprovalFinalizedIntegrationEvent("LeaveRequest", entity.Id, "superseded-approval", ApprovalStatus.Rejected),
            TestContext.Current.CancellationToken);

        // Assert
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task Handle_ShouldBeNoOp_WhenRowIsAlreadyAtTheTargetStatus()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity(LeaveRequestStatus.Approved, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ApprovalFinalizedIntegrationEventHandler(host.Context, Logger);

        // Act
        await handler.Handle(
            new ApprovalFinalizedIntegrationEvent("LeaveRequest", entity.Id, "approval-1", ApprovalStatus.Approved),
            TestContext.Current.CancellationToken);

        // Assert
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Approved, persisted.Status);
    }

    [Fact]
    public async Task Handle_ShouldSwallow_WhenTheDbContextFaults()
    {
        // Arrange — dispose the host so every DbContext call throws ObjectDisposedException
        var host = new LeaveManagementTestHost();
        var entity = MakeEntity(LeaveRequestStatus.Pending, "approval-1");
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var context = host.Context;
        var handler = new ApprovalFinalizedIntegrationEventHandler(context, Logger);
        host.Dispose();

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(
            new ApprovalFinalizedIntegrationEvent("LeaveRequest", entity.Id, "approval-1", ApprovalStatus.Approved),
            TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(exception);
    }
}
