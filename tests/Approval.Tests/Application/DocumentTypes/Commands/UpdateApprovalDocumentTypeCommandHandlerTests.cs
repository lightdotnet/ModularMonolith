using Approval.Tests.TestSupport;
using StarterKit.Approval.Api.Application.DocumentTypes.Commands;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Approval.Contracts.DocumentTypes;
using Xunit;

namespace Approval.Tests.Application.DocumentTypes.Commands;

public class UpdateApprovalDocumentTypeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenDocumentTypeDoesNotExist()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var handler = new UpdateApprovalDocumentTypeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateApprovalDocumentTypeCommand(new ApprovalDocumentTypeDto
            {
                Id = "missing", Name = "Invoice", Code = "INV", IsActive = true,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCodeChangedToOneUsedByAnotherRow()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var a = new ApprovalDocumentType { Name = "Invoice", Code = "INV" };
        var b = new ApprovalDocumentType { Name = "Contract", Code = "CTR" };
        await host.Context.ApprovalDocumentTypes.AddRangeAsync(a, b);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateApprovalDocumentTypeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateApprovalDocumentTypeCommand(new ApprovalDocumentTypeDto
            {
                Id = b.Id, Name = "Contract", Code = "INV", IsActive = true,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldMutate_WhenValid()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        var entity = new ApprovalDocumentType { Name = "Invoice", Code = "INV", IsActive = true };
        await host.Context.ApprovalDocumentTypes.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateApprovalDocumentTypeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateApprovalDocumentTypeCommand(new ApprovalDocumentTypeDto
            {
                Id = entity.Id, Name = "Invoice v2", Code = "INV", Description = "Updated", IsActive = false,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.ApprovalDocumentTypes.FindAsync([entity.Id], TestContext.Current.CancellationToken);
        Assert.Equal("Invoice v2", reloaded!.Name);
        Assert.False(reloaded.IsActive);
    }
}
