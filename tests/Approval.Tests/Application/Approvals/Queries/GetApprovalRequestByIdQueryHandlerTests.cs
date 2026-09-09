using Approval.Tests.TestSupport;
using StarterKit.Approval.Api.Application.Approvals.Queries;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Application.Approvals.Queries;

public class GetApprovalRequestByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var handler = new GetApprovalRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetApprovalRequestByIdQuery("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnRequest_RegardlessOfWhoIsAsking()
    {
        // Arrange: admin surface - no relation check, unlike GetMyApprovalRequestByIdQuery.
        using var host = new ApprovalTestHost();
        var entity = ApprovalEntityBuilder.Request(
            requestType: "Test",
            requestId: "req-1",
            requesterUserId: "requester",
            title: "Title",
            status: ApprovalStatus.Pending,
            currentLevel: 1,
            steps: [ApprovalEntityBuilder.Step(1, "approver-1", "approver-1")],
            requesterEmployeeId: "requester");
        await host.Context.ApprovalRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApprovalRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetApprovalRequestByIdQuery(entity.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(entity.Id, result.Data.Id);
    }

    [Fact]
    public async Task Handle_ShouldFlattenDocumentType_WhenRequestIsTagged()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var documentType = new ApprovalDocumentType { Name = "Leave request", Code = "LEAVE", IsActive = true };
        await host.Context.ApprovalDocumentTypes.AddAsync(documentType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var entity = ApprovalEntityBuilder.Request(
            requestType: "LEAVE",
            requestId: "req-1",
            requesterUserId: "requester",
            title: "Title",
            status: ApprovalStatus.Pending,
            currentLevel: 1,
            steps: [ApprovalEntityBuilder.Step(1, "approver-1", "emp-1", approverName: "Approver One")],
            requesterName: "Requester One",
            documentTypeId: documentType.Id);
        await host.Context.ApprovalRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApprovalRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetApprovalRequestByIdQuery(entity.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(documentType.Id, result.Data.DocumentTypeId);
        Assert.Equal("Leave request", result.Data.DocumentTypeName);
        Assert.Equal("Requester One", result.Data.RequesterName);
        Assert.Equal("Approver One", result.Data.Steps.Single().ApproverName);
    }
}
