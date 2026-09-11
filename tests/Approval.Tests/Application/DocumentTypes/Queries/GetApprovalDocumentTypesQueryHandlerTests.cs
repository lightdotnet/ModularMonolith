using Approval.Tests.TestSupport;
using StarterKit.Approval.Api.Application.DocumentTypes.Queries;
using StarterKit.Approval.Api.Domain.Approvals;
using Xunit;

namespace Approval.Tests.Application.DocumentTypes.Queries;

public class GetApprovalDocumentTypesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDocumentTypes_OrderedByName()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalDocumentTypes.AddRangeAsync(
            new ApprovalDocumentType { Name = "Contract", Code = "CTR" },
            new ApprovalDocumentType { Name = "Agreement", Code = "AGR" },
            new ApprovalDocumentType { Name = "Invoice", Code = "INV" });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApprovalDocumentTypesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetApprovalDocumentTypesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Agreement", "Contract", "Invoice"], result.Select(x => x.Name));
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyActive_WhenActiveOnlyRequested()
    {
        // Arrange
        using var host = new ApprovalTestHost();
        await host.Context.ApprovalDocumentTypes.AddRangeAsync(
            new ApprovalDocumentType { Name = "Contract", Code = "CTR", IsActive = true },
            new ApprovalDocumentType { Name = "Invoice", Code = "INV", IsActive = false });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetApprovalDocumentTypesQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetApprovalDocumentTypesQuery(ActiveOnly: true), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Contract"], result.Select(x => x.Name));
    }
}
