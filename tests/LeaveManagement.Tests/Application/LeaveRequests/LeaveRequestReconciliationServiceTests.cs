using LeaveManagement.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests;

public class LeaveRequestReconciliationServiceTests
{
    private static ApprovalStatusView View(string approvalRequestId, string requestId, ApprovalStatus status) =>
        new(approvalRequestId, "LeaveRequest", requestId, status, 1);

    private static async Task<LeaveRequest> SeedAsync(
        LeaveManagementTestHost host,
        DateTimeOffset created,
        LeaveRequestStatus status,
        string? approvalRequestId)
    {
        host.DateTime.UtcNow = created;
        var entity = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, created, created.AddDays(1), status,
            approvalRequestId: approvalRequestId);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entity;
    }

    private static Mock<IApprovalService> ApprovalServiceReturning(
        Action<IReadOnlyCollection<string>> onCall,
        IReadOnlyDictionary<string, ApprovalStatusView> views)
    {
        var mock = new Mock<IApprovalService>();
        mock
            .Setup(s => s.GetStatusesByRequestAsync(
                "LeaveRequest", It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Returns((string _, IReadOnlyCollection<string> ids, CancellationToken _) =>
            {
                onCall(ids);
                return Task.FromResult(views);
            });
        return mock;
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldFlipTerminalRows_LeavePendingOnes_AndSkipRowsWithoutApproval()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var a = await SeedAsync(host, baseTime, LeaveRequestStatus.Pending, "appr-a");
        var b = await SeedAsync(host, baseTime.AddMinutes(1), LeaveRequestStatus.Pending, "appr-b");
        var c = await SeedAsync(host, baseTime.AddMinutes(2), LeaveRequestStatus.Pending, "appr-c");
        await SeedAsync(host, baseTime.AddMinutes(3), LeaveRequestStatus.Pending, null);
        await SeedAsync(host, baseTime.AddMinutes(4), LeaveRequestStatus.Approved, "appr-e");

        IReadOnlyCollection<string> requestedIds = [];
        var views = new Dictionary<string, ApprovalStatusView>
        {
            [a.Id] = View("appr-a", a.Id, ApprovalStatus.Approved),
            [b.Id] = View("appr-b", b.Id, ApprovalStatus.Pending),
            [c.Id] = View("appr-c", c.Id, ApprovalStatus.Rejected),
        };
        var approvalService = ApprovalServiceReturning(ids => requestedIds = ids, views);

        // Act
        var changed = await LeaveRequestReconciliationService.ReconcileOnceAsync(
            host.Context, approvalService.Object, batchSize: 50, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, changed);
        approvalService.Verify(
            s => s.GetStatusesByRequestAsync(
                "LeaveRequest", It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(new[] { a.Id, b.Id, c.Id }, requestedIds);

        var rows = await host.Context.LeaveRequests
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Status, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Approved, rows[a.Id]);
        Assert.Equal(LeaveRequestStatus.Pending, rows[b.Id]);
        Assert.Equal(LeaveRequestStatus.Rejected, rows[c.Id]);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRespectBatchSize_AndOldestCreatedFirst()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var baseTime = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var oldest = await SeedAsync(host, baseTime, LeaveRequestStatus.Pending, "appr-1");
        var middle = await SeedAsync(host, baseTime.AddMinutes(1), LeaveRequestStatus.Pending, "appr-2");
        var newest = await SeedAsync(host, baseTime.AddMinutes(2), LeaveRequestStatus.Pending, "appr-3");

        IReadOnlyCollection<string> requestedIds = [];
        var views = new Dictionary<string, ApprovalStatusView>
        {
            [oldest.Id] = View("appr-1", oldest.Id, ApprovalStatus.Approved),
            [middle.Id] = View("appr-2", middle.Id, ApprovalStatus.Approved),
            [newest.Id] = View("appr-3", newest.Id, ApprovalStatus.Approved),
        };
        var approvalService = ApprovalServiceReturning(ids => requestedIds = ids, views);

        // Act
        var changed = await LeaveRequestReconciliationService.ReconcileOnceAsync(
            host.Context, approvalService.Object, batchSize: 2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, changed);
        Assert.Equal(new[] { oldest.Id, middle.Id }, requestedIds);

        var rows = await host.Context.LeaveRequests
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Status, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Approved, rows[oldest.Id]);
        Assert.Equal(LeaveRequestStatus.Approved, rows[middle.Id]);
        Assert.Equal(LeaveRequestStatus.Pending, rows[newest.Id]);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldLeaveRowUnchanged_WhenReturnedViewBelongsToASupersededWorkflow()
    {
        // Arrange — the row has since been resubmitted against a fresh workflow; a status view for
        // the old (superseded) approval id must not be applied. Mirrors the equivalent mismatched-id
        // guard already covered at the entity level (LeaveRequestTests), the integration-event
        // subscriber level (ApprovalFinalizedIntegrationEventHandlerTests), and the coordinator's own
        // ReconcileStatusAsync (LeaveRequestApprovalCoordinatorTests).
        using var host = new LeaveManagementTestHost();
        var entity = await SeedAsync(host, DateTimeOffset.UtcNow, LeaveRequestStatus.Pending, "current-approval");

        var views = new Dictionary<string, ApprovalStatusView>
        {
            [entity.Id] = View("superseded-approval", entity.Id, ApprovalStatus.Approved),
        };
        var approvalService = ApprovalServiceReturning(_ => { }, views);

        // Act
        var changed = await LeaveRequestReconciliationService.ReconcileOnceAsync(
            host.Context, approvalService.Object, batchSize: 50, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, changed);
        var persisted = await host.Context.LeaveRequests
            .AsNoTracking()
            .FirstAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.Equal(LeaveRequestStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldReturnZero_AndNotCallApproval_WhenNoCandidateRows()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        await SeedAsync(host, DateTimeOffset.UtcNow, LeaveRequestStatus.Approved, "appr-1");
        var approvalService = new Mock<IApprovalService>();

        // Act
        var changed = await LeaveRequestReconciliationService.ReconcileOnceAsync(
            host.Context, approvalService.Object, batchSize: 50, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, changed);
        approvalService.Verify(
            s => s.GetStatusesByRequestAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
