using Approval.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using StarterKit.Approval.Api.Services;
using StarterKit.Approval.Contracts.Approvals;
using Xunit;

namespace Approval.Tests.Services;

public class ApprovalServiceReservedTypeTests
{
    private static ApprovalService CreateService(ApprovalTestHost host) =>
        new(
            host.Context,
            host.Publisher,
            host.DateTime,
            NullLogger<ApprovalService>.Instance);

    private static CreateApprovalRequest NewRequest(string requestType, string requestId) =>
        new(
            RequestType: requestType,
            RequestId: requestId,
            RequesterUserId: "requester-1",
            RequesterEmployeeId: null,
            RequesterName: null,
            Title: "Title",
            Content: null,
            DeepLinkUrl: null,
            DocumentTypeId: null,
            ApproverChain: [new ApproverStepInput(1, "approver-1", "approver-1")]);

    [Fact]
    public async Task CreateAsync_ShouldStillAccept_ReservedTypes_WhenCalledInProcessByTheOwningModule()
    {
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        var result = await service.CreateAsync(
            NewRequest(ApprovalRequestTypes.PurchaseOrder, "42"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnTheGenuineWorkflow_EvenWhenANewerRequestNamesTheSameRecord()
    {
        using var host = new ApprovalTestHost();
        var service = CreateService(host);
        var genuine = await service.CreateAsync(
            NewRequest(ApprovalRequestTypes.PurchaseOrder, "42"),
            TestContext.Current.CancellationToken);
        host.DateTime.UtcNow = host.DateTime.UtcNow.AddMinutes(5);
        var forged = await service.CreateAsync(
            NewRequest(ApprovalRequestTypes.PurchaseOrder, "42"),
            TestContext.Current.CancellationToken);

        var byId = await service.GetStatusAsync(genuine.Data!, TestContext.Current.CancellationToken);
        var byIds = await service.GetStatusesAsync([genuine.Data!], TestContext.Current.CancellationToken);
        var newestByRecord = await service.GetStatusByRequestAsync(
            ApprovalRequestTypes.PurchaseOrder,
            "42",
            TestContext.Current.CancellationToken);

        Assert.Equal(genuine.Data, byId!.ApprovalRequestId);
        Assert.Equal(genuine.Data, byIds[genuine.Data!].ApprovalRequestId);
        Assert.Single(byIds);
        // Documents the hazard the id lookup avoids: the (type, id) lookup returns the newest row.
        Assert.Equal(forged.Data, newestByRecord!.ApprovalRequestId);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnNull_WhenTheIdIsUnknown()
    {
        using var host = new ApprovalTestHost();
        var service = CreateService(host);

        Assert.Null(await service.GetStatusAsync("missing", TestContext.Current.CancellationToken));
        Assert.Empty(await service.GetStatusesAsync([], TestContext.Current.CancellationToken));
    }
}
