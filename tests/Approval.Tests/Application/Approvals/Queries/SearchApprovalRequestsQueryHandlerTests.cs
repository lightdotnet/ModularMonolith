using Approval.Tests.TestSupport;
using Light.Contracts;
using StarterKit.Approval.Api.Application.Approvals.Queries;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Application.Approvals.Queries;

public class SearchApprovalRequestsQueryHandlerTests
{
    private static ApprovalRequest NewRequest(string requestType, string title, ApprovalStatus status) =>
        ApprovalEntityBuilder.Request(
            requestType: requestType,
            requestId: Guid.NewGuid().ToString(),
            requesterUserId: "requester",
            title: title,
            status: status,
            currentLevel: 1,
            steps: [ApprovalEntityBuilder.Step(1, "approver-1", "approver-1")],
            requesterEmployeeId: "requester");

    [Fact]
    public async Task Handle_ShouldReturnEveryRequest_RegardlessOfRequesterOrApprover()
    {
        // Arrange: this is the admin, unrestricted search - unlike SearchMyApprovalsQuery it has
        // no notion of "the current user" at all.
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalRequests.AddRangeAsync(
            NewRequest("Leave", "Leave A", ApprovalStatus.Pending),
            NewRequest("Expense", "Expense B", ApprovalStatus.Approved));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchApprovalRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchApprovalRequestsQuery(new ApprovalRequestSearchRequest()), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Data.TotalRecords);
    }

    [Fact]
    public async Task Handle_ShouldFilterByRequestTypeAndStatus()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalRequests.AddRangeAsync(
            NewRequest("Leave", "Leave A", ApprovalStatus.Pending),
            NewRequest("Leave", "Leave B", ApprovalStatus.Approved),
            NewRequest("Expense", "Expense C", ApprovalStatus.Pending));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new SearchApprovalRequestsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchApprovalRequestsQuery(
                new ApprovalRequestSearchRequest { RequestType = "Leave", Status = ApprovalStatus.Pending }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Leave A", result.Data.Records.Single().Title);
    }
}
