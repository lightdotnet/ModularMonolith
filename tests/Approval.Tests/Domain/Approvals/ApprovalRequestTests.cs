using Approval.Tests.TestSupport;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Domain.Approvals;

/// <summary>
/// Pure-aggregate coverage for the two invariants added in the approval-hardening pass — blank
/// <c>requestType</c>/<c>requestId</c> rejection on <see cref="ApprovalRequest.Create"/> and the
/// non-pending current-step guard on <see cref="ApprovalRequest.Decide"/>. Not a full aggregate suite.
/// </summary>
public class ApprovalRequestTests
{
    private static IReadOnlyList<ApproverStepInput> OneApprover() =>
        [new ApproverStepInput(1, "approver-user-1", "approver-emp-1", "Approver One")];

    [Theory]
    [InlineData("", "req-1")]
    [InlineData("   ", "req-1")]
    [InlineData("LeaveRequest", "")]
    [InlineData("LeaveRequest", "   ")]
    public void Create_ShouldFail_WhenRequestTypeOrRequestIdIsBlank(string requestType, string requestId)
    {
        var result = ApprovalRequest.Create(
            requestType,
            requestId,
            requesterUserId: "requester-1",
            requesterEmployeeId: null,
            requesterName: null,
            title: "Title",
            content: null,
            deepLinkUrl: null,
            documentTypeId: null,
            approverChain: OneApprover());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenRequestTypeAndRequestIdArePresent()
    {
        var result = ApprovalRequest.Create(
            requestType: "LeaveRequest",
            requestId: "req-1",
            requesterUserId: "requester-1",
            requesterEmployeeId: null,
            requesterName: null,
            title: "Title",
            content: null,
            deepLinkUrl: null,
            documentTypeId: null,
            approverChain: OneApprover());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Decide_ShouldFail_WhenTheResolvedCurrentStepIsNotPending()
    {
        var request = ApprovalEntityBuilder.Request(
            requestType: "LeaveRequest",
            requestId: "req-1",
            requesterUserId: "requester-1",
            title: "Title",
            status: ApprovalStatus.Pending,
            currentLevel: 1,
            steps:
            [
                ApprovalEntityBuilder.Step(1, "approver-user-1", "approver-emp-1", ApprovalStepStatus.Approved),
            ]);

        var result = request.Decide(
            decidedByUserId: "approver-user-1",
            approved: true,
            comment: null,
            decidedAt: DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Contains("no longer pending", result.Message);
    }
}
