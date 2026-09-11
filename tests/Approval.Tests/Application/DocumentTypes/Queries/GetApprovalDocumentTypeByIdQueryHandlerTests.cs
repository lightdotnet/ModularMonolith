using Approval.Tests.TestSupport;
using StarterKit.Approval.Api.Application.DocumentTypes.Queries;
using StarterKit.Approval.Api.Domain.Approvals;
using Xunit;

namespace Approval.Tests.Application.DocumentTypes.Queries;

public class GetApprovalDocumentTypeByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenDocumentTypeDoesNotExist()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var handler = new GetApprovalDocumentTypeByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetApprovalDocumentTypeByIdQuery("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnDto_WhenDocumentTypeExists()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var entity = new ApprovalDocumentType { Name = "Invoice", Code = "INV" };
        await host.Context.ApprovalDocumentTypes.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApprovalDocumentTypeByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetApprovalDocumentTypeByIdQuery(entity.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(entity.Id, result.Data.Id);
    }
}
