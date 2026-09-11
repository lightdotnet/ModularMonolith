using Approval.Tests.TestSupport;
using StarterKit.Approval.Api.Application.DocumentTypes.Commands;
using StarterKit.Approval.Api.Domain.Approvals;
using Xunit;

namespace Approval.Tests.Application.DocumentTypes.Commands;

public class DeleteApprovalDocumentTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenDocumentTypeDoesNotExist()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var handler = new DeleteApprovalDocumentTypeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteApprovalDocumentTypeCommand("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenDocumentTypeIsInUse()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var docType = new ApprovalDocumentType { Name = "Invoice", Code = "INV" };
        await host.Context.ApprovalDocumentTypes.AddAsync(docType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.ApprovalRequests.AddAsync(
            ApprovalEntityBuilder.Request(
                requestType: "Test",
                requestId: "req-1",
                requesterUserId: "requester",
                title: "Title",
                requesterEmployeeId: "emp-1",
                documentTypeId: docType.Id),
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteApprovalDocumentTypeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteApprovalDocumentTypeCommand(docType.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldDelete_WhenNotInUse()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var docType = new ApprovalDocumentType { Name = "Invoice", Code = "INV" };
        await host.Context.ApprovalDocumentTypes.AddAsync(docType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteApprovalDocumentTypeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteApprovalDocumentTypeCommand(docType.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(host.Context.ApprovalDocumentTypes.Any(x => x.Id == docType.Id));
    }
}
