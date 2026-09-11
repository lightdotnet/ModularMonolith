using Approval.Tests.TestSupport;
using Light.Exceptions;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Domain.Approvals;

/// <summary>
/// Pure-aggregate coverage for the two invariants added in the approval-hardening pass — blank
/// <c>requestType</c>/<c>requestId</c> rejection on <see cref="ApprovalRequest.Create"/> and the
/// non-pending current-step guard on <see cref="ApprovalRequest.Decide"/>. Both now surface as
/// thrown <c>Light.Exceptions</c> types rather than a failed <c>Result</c>. Not a full aggregate suite.
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
    public void Create_ShouldThrowValidationException_WhenRequestTypeOrRequestIdIsBlank(string requestType, string requestId)
    {
        // Arrange
        var approverChain = OneApprover();

        // Act & Assert
        Assert.Throws<ValidationException>(() => ApprovalRequest.Create(
            requestType,
            requestId,
            requesterUserId: "requester-1",
            requesterEmployeeId: null,
            requesterName: null,
            title: "Title",
            content: null,
            deepLinkUrl: null,
            documentTypeId: null,
            approverChain: approverChain));
    }

    [Fact]
    public void Create_ShouldSucceed_WhenRequestTypeAndRequestIdArePresent()
    {
        // Arrange & Act
        var request = ApprovalRequest.Create(
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

        // Assert
        Assert.NotNull(request);
        Assert.Equal(ApprovalStatus.Pending, request.Status);
    }

    [Fact]
    public void Decide_ShouldThrowConflictException_WhenTheResolvedCurrentStepIsNotPending()
    {
        // Arrange
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

        // Act
        var ex = Assert.Throws<ConflictException>(() => request.Decide(
            decidedByUserId: "approver-user-1",
            approved: true,
            comment: null,
            decidedAt: DateTimeOffset.UtcNow));

        // Assert
        Assert.Contains("no longer pending", ex.Message);
    }
}
