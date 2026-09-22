using StarterKit.Approval.Api.Application.Approvals.Commands;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Application.Approvals.Commands;

public class CreateApprovalRequestCommandValidatorTests
{
    private static CreateApprovalRequestCommand NewCommand(string requestType) =>
        new(new CreateApprovalRequest(
            RequestType: requestType,
            RequestId: "req-1",
            RequesterUserId: "requester-1",
            RequesterEmployeeId: null,
            RequesterName: null,
            Title: "Title",
            Content: null,
            DeepLinkUrl: null,
            DocumentTypeId: null,
            ApproverChain: [new ApproverStepInput(1, "approver-1", "approver-1")]));

    [Theory]
    [InlineData(ApprovalRequestTypes.LeaveRequest)]
    [InlineData(ApprovalRequestTypes.PurchaseOrder)]
    [InlineData("purchaseorder")]
    [InlineData("  LEAVEREQUEST ")]
    public void Validate_ShouldReject_ModuleOwnedRequestTypes(string requestType)
    {
        var result = new CreateApprovalRequestCommandValidator().Validate(NewCommand(requestType));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("reserved"));
    }

    [Theory]
    [InlineData("General")]
    [InlineData("Expense")]
    [InlineData("Leave")]
    public void Validate_ShouldAccept_GenericRequestTypes(string requestType)
    {
        var result = new CreateApprovalRequestCommandValidator().Validate(NewCommand(requestType));

        Assert.True(result.IsValid);
    }
}
