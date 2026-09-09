using Approval.Tests.TestSupport;
using Light.Contracts;
using StarterKit.Approval.Api.Application.Approvals.Queries;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Application.Approvals.Queries;

public class SearchMyApprovalsQueryHandlerTests
{
    private static ApprovalRequest NewRequest(
        string requesterUserId, ApprovalStatus status, int currentLevel, params ApprovalStep[] steps) =>
        ApprovalEntityBuilder.Request(
            requestType: "Test",
            requestId: Guid.NewGuid().ToString(),
            requesterUserId: requesterUserId,
            title: "Title",
            status: status,
            currentLevel: currentLevel,
            steps: steps,
            requesterEmployeeId: requesterUserId);

    private static ApprovalStep NewStep(
        int level, string approverUserId, ApprovalStepStatus status, DateTimeOffset? decidedAt = null) =>
        ApprovalEntityBuilder.Step(
            level: level,
            approverUserId: approverUserId,
            approverEmployeeId: approverUserId,
            status: status,
            decidedAt: decidedAt);

    [Fact]
    public async Task Handle_Requested_ShouldReturnOnlyRequestsCreatedByTheUser()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalRequests.AddRangeAsync(
            NewRequest("user-1", ApprovalStatus.Pending, 1, NewStep(1, "approver-1", ApprovalStepStatus.Pending)),
            NewRequest("user-2", ApprovalStatus.Pending, 1, NewStep(1, "approver-1", ApprovalStepStatus.Pending)));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchMyApprovalsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchMyApprovalsQuery("user-1", new MyApprovalRequestSearchRequest { Relation = ApprovalRelation.Requested }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("user-1", result.Data.Records.Single().RequesterUserId);
    }

    [Fact]
    public async Task Handle_AwaitingMyDecision_ShouldReturnOnlyPendingStepsAtCurrentLevelAssignedToTheUser()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalRequests.AddRangeAsync(
            // Currently awaiting user-1 at level 1
            NewRequest("requester", ApprovalStatus.Pending, 1, NewStep(1, "user-1", ApprovalStepStatus.Pending)),
            // user-1 is an approver, but at a future level not yet reached
            NewRequest(
                "requester", ApprovalStatus.Pending, 1,
                NewStep(1, "someone-else", ApprovalStepStatus.Pending),
                NewStep(2, "user-1", ApprovalStepStatus.Pending)),
            // Already decided by user-1 - no longer "awaiting"
            NewRequest(
                "requester", ApprovalStatus.Approved, 1,
                NewStep(1, "user-1", ApprovalStepStatus.Approved, DateTimeOffset.UtcNow)));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchMyApprovalsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchMyApprovalsQuery("user-1", new MyApprovalRequestSearchRequest { Relation = ApprovalRelation.AwaitingMyDecision }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
    }

    [Fact]
    public async Task Handle_DecidedByMe_ShouldReturnRequestsWhereTheUserDecidedAStep_RegardlessOfCurrentLevel()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalRequests.AddRangeAsync(
            // user-1 decided level 1, request has since moved on to level 2
            NewRequest(
                "requester", ApprovalStatus.Pending, 2,
                NewStep(1, "user-1", ApprovalStepStatus.Approved, DateTimeOffset.UtcNow),
                NewStep(2, "someone-else", ApprovalStepStatus.Pending)),
            // user-1 is assigned but hasn't decided yet
            NewRequest("requester", ApprovalStatus.Pending, 1, NewStep(1, "user-1", ApprovalStepStatus.Pending)));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchMyApprovalsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchMyApprovalsQuery("user-1", new MyApprovalRequestSearchRequest { Relation = ApprovalRelation.DecidedByMe }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal(2, result.Data.Records.Single().CurrentLevel);
    }

    [Fact]
    public async Task Handle_All_ShouldReturnRequestsWhereTheUserIsRequesterOrApproverOnAnyStep()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalRequests.AddRangeAsync(
            NewRequest("user-1", ApprovalStatus.Pending, 1, NewStep(1, "someone-else", ApprovalStepStatus.Pending)),
            NewRequest(
                "requester", ApprovalStatus.Pending, 1,
                NewStep(1, "someone-else", ApprovalStepStatus.Pending),
                NewStep(2, "user-1", ApprovalStepStatus.Pending)),
            NewRequest("unrelated", ApprovalStatus.Pending, 1, NewStep(1, "someone-else", ApprovalStepStatus.Pending)));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchMyApprovalsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchMyApprovalsQuery("user-1", new MyApprovalRequestSearchRequest { Relation = ApprovalRelation.All }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Data.TotalRecords);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchValue_MatchingTitle()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var matching = NewRequest("user-1", ApprovalStatus.Pending, 1, NewStep(1, "approver-1", ApprovalStepStatus.Pending));
        ApprovalEntityBuilder.SetTitle(matching, "Annual leave request");
        var nonMatching = NewRequest("user-1", ApprovalStatus.Pending, 1, NewStep(1, "approver-1", ApprovalStepStatus.Pending));
        ApprovalEntityBuilder.SetTitle(nonMatching, "Expense report");
        await host.Context.ApprovalRequests.AddRangeAsync(matching, nonMatching);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchMyApprovalsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchMyApprovalsQuery(
                "user-1",
                new MyApprovalRequestSearchRequest { Relation = ApprovalRelation.Requested, SearchValue = "leave" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
    }
}
