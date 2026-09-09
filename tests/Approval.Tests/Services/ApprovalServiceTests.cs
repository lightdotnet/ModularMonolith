using Approval.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Api.Services;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Services;

public class ApprovalServiceTests
{
    private static ApprovalService CreateService(ApprovalTestHost host) =>
        new(
            host.Context,
            host.Publisher,
            host.DateTime,
            NullLogger<ApprovalService>.Instance);

    private static CreateApprovalRequest NewRequest(params (int Level, string ApproverUserId)[] chain) =>
        new(
            RequestType: "Test",
            RequestId: "req-1",
            RequesterUserId: "requester-1",
            RequesterEmployeeId: "emp-1",
            RequesterName: "Requester One",
            Title: "Title",
            Content: "Content",
            DeepLinkUrl: null,
            DocumentTypeId: null,
            ApproverChain: chain
                .Select(c => new ApproverStepInput(
                    c.Level, c.ApproverUserId, c.ApproverUserId, $"Approver {c.Level}"))
                .ToList());

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenChainIsEmpty()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        // Act
        var result = await service.CreateAsync(NewRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.DoesNotContain(host.Publisher.Published, e => e is ApprovalStepPendingEvent);
    }

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenChainLevelsAreDuplicated()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        // Act
        var result = await service.CreateAsync(
            NewRequest((1, "approver-1"), (1, "approver-2")), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.DoesNotContain(host.Publisher.Published, e => e is ApprovalStepPendingEvent);
    }

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenChainLevelIsNotPositive()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        // Act
        var result = await service.CreateAsync(
            NewRequest((0, "approver-1")), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.DoesNotContain(host.Publisher.Published, e => e is ApprovalStepPendingEvent);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreatePendingRequest_AndNotifyFirstApprover()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        // Act
        var result = await service.CreateAsync(
            NewRequest((1, "approver-1"), (2, "approver-2")), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.ApprovalRequests
            .Include(x => x.Steps)
            .SingleAsync(x => x.Id == result.Data, TestContext.Current.CancellationToken);
        Assert.Equal(ApprovalStatus.Pending, entity.Status);
        Assert.Equal(1, entity.CurrentLevel);
        // Display labels supplied by the caller are persisted verbatim (Approval cannot resolve them).
        Assert.Equal("Requester One", entity.RequesterName);
        Assert.Equal("Approver 1", entity.Steps.Single(s => s.Level == 1).ApproverName);
        Assert.Contains(
            host.Publisher.Published.OfType<ApprovalStepPendingEvent>(),
            e => e.ApprovalRequestId == result.Data && e.ApproverUserId == "approver-1");
    }

    private static async Task<(ApprovalTestHost Host, string RequestId)> SeedTwoLevelRequestAsync()
    {
        var host = new ApprovalTestHost();
        var service = CreateService(host);
        var created = await service.CreateAsync(
            NewRequest((1, "approver-1"), (2, "approver-2")), TestContext.Current.CancellationToken);
        host.Publisher.Clear();
        return (host, created.Data!);
    }

    [Fact]
    public async Task DecideAsync_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        // Act
        var result = await service.DecideAsync(
            "missing", "approver-1", true, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DecideAsync_ShouldReject_WhenCallerIsNotTheAssignedApprover()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);

        // Act
        var result = await service.DecideAsync(
            requestId, "someone-else", true, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DecideAsync_ShouldRequireComment_WhenRejecting()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);

        // Act
        var result = await service.DecideAsync(
            requestId, "approver-1", false, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DecideAsync_ShouldAdvanceLevel_AndNotifyNextApprover_WhenApprovedAndMoreLevelsRemain()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);

        // Act
        var result = await service.DecideAsync(
            requestId, "approver-1", true, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.ApprovalRequests.FindAsync([requestId], TestContext.Current.CancellationToken);
        Assert.Equal(ApprovalStatus.Pending, entity!.Status);
        Assert.Equal(2, entity.CurrentLevel);
        Assert.Contains(
            host.Publisher.Published.OfType<ApprovalStepPendingEvent>(),
            e => e.ApproverUserId == "approver-2");
        Assert.DoesNotContain(host.Publisher.Published, e => e is ApprovalFinalizedEvent);
    }

    [Fact]
    public async Task DecideAsync_ShouldFinalizeAsApproved_WhenLastLevelApproves()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);
        await service.DecideAsync(requestId, "approver-1", true, null, TestContext.Current.CancellationToken);

        // Act
        var result = await service.DecideAsync(
            requestId, "approver-2", true, "Looks good", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.ApprovalRequests.FindAsync([requestId], TestContext.Current.CancellationToken);
        Assert.Equal(ApprovalStatus.Approved, entity!.Status);
        Assert.NotNull(entity.FinalizedAt);
        Assert.Contains(
            host.Publisher.Published.OfType<ApprovalFinalizedEvent>(),
            e => e.Status == ApprovalStatus.Approved && e.DecidedByUserId == "approver-2");
    }

    [Fact]
    public async Task DecideAsync_ShouldFinalizeAsRejected_AndStopAdvancing()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);

        // Act
        var result = await service.DecideAsync(
            requestId, "approver-1", false, "Not compliant", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.ApprovalRequests.FindAsync([requestId], TestContext.Current.CancellationToken);
        Assert.Equal(ApprovalStatus.Rejected, entity!.Status);
        Assert.Equal(1, entity.CurrentLevel);
        Assert.Contains(
            host.Publisher.Published.OfType<ApprovalFinalizedEvent>(),
            e => e.Status == ApprovalStatus.Rejected);
    }

    [Fact]
    public async Task DecideAsync_ShouldReject_WhenRequestAlreadyFinalized()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);
        await service.DecideAsync(requestId, "approver-1", false, "Not compliant", TestContext.Current.CancellationToken);

        // Act
        var result = await service.DecideAsync(
            requestId, "approver-2", true, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        // Act
        var result = await service.CancelAsync("missing", "requester-1", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CancelAsync_ShouldReject_WhenCallerIsNotTheRequester()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);

        // Act
        var result = await service.CancelAsync(requestId, "approver-1", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CancelAsync_ShouldReject_WhenRequestIsNotPending()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);
        await service.DecideAsync(requestId, "approver-1", false, "Not compliant", TestContext.Current.CancellationToken);

        // Act
        var result = await service.CancelAsync(requestId, "requester-1", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CancelAsync_ShouldCancel_WhenRequesterCancelsPendingRequest()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);

        // Act
        var result = await service.CancelAsync(requestId, "requester-1", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.ApprovalRequests.FindAsync([requestId], TestContext.Current.CancellationToken);
        Assert.Equal(ApprovalStatus.Cancelled, entity!.Status);
        Assert.NotNull(entity.FinalizedAt);
    }

    [Fact]
    public async Task GetByRequestAsync_ShouldReturnNull_WhenNoMatchExists()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        // Act
        var dto = await service.GetByRequestAsync("Leave", "missing", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public async Task GetByRequestAsync_ShouldReturnDto_WhenMatchExists()
    {
        // Arrange
        var (host, requestId) = await SeedTwoLevelRequestAsync();
        using var _ = host;
        var service = CreateService(host);

        // Act
        var dto = await service.GetByRequestAsync("Test", "req-1", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(requestId, dto!.Id);
    }

    [Fact]
    public async Task GetByRequestAsync_ShouldReturnMostRecent_WhenMultipleMatchExist()
    {
        // Arrange: (RequestType, RequestId) is not unique - a re-submission after a rejection
        // leaves two rows. The query must return the latest rather than throw.
        using var host = new ApprovalTestHost();

        host.DateTime.UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await host.Context.ApprovalRequests.AddAsync(
            ApprovalEntityBuilder.Request(
                requestType: "Leave",
                requestId: "L-1",
                requesterUserId: "u1",
                title: "First",
                status: ApprovalStatus.Rejected,
                currentLevel: 1,
                steps: [ApprovalEntityBuilder.Step(1, "a1", "e1")]),
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        host.DateTime.UtcNow = host.DateTime.UtcNow.AddDays(1);
        await host.Context.ApprovalRequests.AddAsync(
            ApprovalEntityBuilder.Request(
                requestType: "Leave",
                requestId: "L-1",
                requesterUserId: "u1",
                title: "Second",
                status: ApprovalStatus.Pending,
                currentLevel: 1,
                steps: [ApprovalEntityBuilder.Step(1, "a1", "e1")]),
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(host);

        // Act
        var dto = await service.GetByRequestAsync("Leave", "L-1", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("Second", dto!.Title);
    }
}
