using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Purchasing.Tests.TestSupport;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Catalog.Contracts.Services;
using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.Locations;
using StarterKit.Locations.Contracts.Services;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Services;
using ValidationException = Light.Exceptions.ValidationException;

namespace Purchasing.Tests.Application.PurchaseOrders;

/// <summary>Create/edit/line/cancel/close/receive handlers against a Sqlite context with Inventory, Catalog and Location mocked.</summary>
public class PurchaseOrderCommandHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string Requester = PurchasingBuilder.Requester;

    private static async Task<PurchaseOrder> ReloadAsync(
        PurchasingTestHost host,
        long id)
    {
        PurchasingSeed.Detach(host);

        return await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == id, Ct);
    }

    private static Mock<ILocationDirectoryService> MakeLocations(LocationStatus status = LocationStatus.Active)
    {
        var mock = new Mock<ILocationDirectoryService>();

        mock.Setup(x => x.GetAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocationDto { Id = "loc-1", Name = "Main WH", Status = status });

        return mock;
    }

    private static CreatePurchaseOrderCommandHandler MakeCreate(
        PurchasingTestHost host,
        Mock<ILocationDirectoryService>? locations = null) =>
        new(
            host.Context,
            new ReceivingLocationResolver((locations ?? MakeLocations()).Object),
            host.DateTime,
            NullLogger<CreatePurchaseOrderCommandHandler>.Instance);

    // Create

    [Fact]
    public async Task Create_ShouldPersistADraft_WithSnapshottedNames_AndTheRequester()
    {
        using var host = new PurchasingTestHost();
        var supplier = await PurchasingSeed.SupplierAsync(host);

        var result = await MakeCreate(host).Handle(
            new CreatePurchaseOrderCommand(
                new CreatePurchaseOrderRequest { SupplierId = supplier.Id, LocationId = " loc-1 ", Note = "n" },
                Requester,
                "emp-1"),
            Ct);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host, result.Data);
        Assert.Equal(PurchaseOrderStatus.Draft, stored.Status);
        Assert.Equal("Main WH", stored.LocationName);
        Assert.Equal("Supplier", stored.SupplierName);
        Assert.Equal(Requester, stored.RequesterUserId);
        Assert.Equal("emp-1", stored.RequesterEmployeeId);
    }

    [Fact]
    public async Task Create_ShouldFail_ForAMissingEmployeeLink_UnknownOrInactiveSupplier_OrABadLocation()
    {
        using var host = new PurchasingTestHost();
        var supplier = await PurchasingSeed.SupplierAsync(host);
        var request = new CreatePurchaseOrderRequest { SupplierId = supplier.Id, LocationId = "loc-1" };

        Assert.False((await MakeCreate(host).Handle(new CreatePurchaseOrderCommand(request, Requester, null), Ct)).IsSuccess);
        Assert.False((await MakeCreate(host).Handle(
            new CreatePurchaseOrderCommand(request with { SupplierId = 999 }, Requester, "e"), Ct)).IsSuccess);
        Assert.False((await MakeCreate(host, MakeLocations(LocationStatus.Inactive)).Handle(
            new CreatePurchaseOrderCommand(request, Requester, "e"), Ct)).IsSuccess);

        supplier.Deactivate();
        await host.Context.SaveChangesAsync(Ct);
        var inactive = await MakeCreate(host).Handle(new CreatePurchaseOrderCommand(request, Requester, "e"), Ct);

        Assert.False(inactive.IsSuccess);
        Assert.False(await host.Context.PurchaseOrders.AnyAsync(Ct));
    }

    // Header and lines: owner / manager rule

    [Fact]
    public async Task UpdateHeader_ShouldAllowTheRequesterOrAManager_AndRefuseAStranger()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var supplier = await PurchasingSeed.SupplierAsync(host, "OTHER");
        var handler = new UpdatePurchaseOrderCommandHandler(
            host.Context,
            new ReceivingLocationResolver(MakeLocations().Object));
        var model = new UpdatePurchaseOrderRequest { SupplierId = supplier.Id, LocationId = "loc-1", Note = "edited" };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UpdatePurchaseOrderCommand(order.Id, model, "stranger", false), Ct));

        Assert.True((await handler.Handle(new UpdatePurchaseOrderCommand(order.Id, model, Requester, false), Ct)).IsSuccess);
        Assert.True((await handler.Handle(new UpdatePurchaseOrderCommand(order.Id, model with { Note = "by manager" }, "stranger", true), Ct)).IsSuccess);
        Assert.Equal("by manager", (await ReloadAsync(host, order.Id)).Note);
    }

    private static Mock<ICatalogPricingService> MakeCatalog(bool exists = true)
    {
        var mock = new Mock<ICatalogPricingService>();

        mock.Setup(x => x.GetPriceInfoAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists ? new ProductPriceInfoDto { Id = 5, ProductName = "Widget", Sku = null } : null);

        return mock;
    }

    [Fact]
    public async Task AddLine_ShouldSnapshotTheProduct_EnforceOwnership_AndReturnNotFoundForUnknownProducts()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var request = new AddPurchaseOrderLineRequest { ProductId = 5, Quantity = 3, UnitCost = 2.5m };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new AddPurchaseOrderLineCommandHandler(host.Context, MakeCatalog().Object)
                .Handle(new AddPurchaseOrderLineCommand(order.Id, request, "stranger", false), Ct));
        Assert.False((await new AddPurchaseOrderLineCommandHandler(host.Context, MakeCatalog(false).Object)
            .Handle(new AddPurchaseOrderLineCommand(order.Id, request, Requester, false), Ct)).IsSuccess);

        var ok = await new AddPurchaseOrderLineCommandHandler(host.Context, MakeCatalog().Object)
            .Handle(new AddPurchaseOrderLineCommand(order.Id, request, Requester, false), Ct);

        Assert.True(ok.IsSuccess);
        var line = (await ReloadAsync(host, order.Id)).Lines.Single(x => x.ProductId == 5);
        Assert.Equal("Widget", line.ProductName);
        Assert.Equal(string.Empty, line.Sku);
        Assert.Equal(2.5m, line.UnitCostAmount);
    }

    [Fact]
    public async Task UpdateLine_ShouldChangeQuantityAndCost_AndEnforceOwnership()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host, (1, 10));
        var lineId = order.Lines[0].Id;
        var handler = new UpdatePurchaseOrderLineCommandHandler(host.Context);
        var model = new UpdatePurchaseOrderLineRequest { Quantity = 4, UnitCost = 9m };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UpdatePurchaseOrderLineCommand(order.Id, lineId, model, "stranger", false), Ct));
        Assert.True((await handler.Handle(new UpdatePurchaseOrderLineCommand(order.Id, lineId, model, Requester, false), Ct)).IsSuccess);

        var line = (await ReloadAsync(host, order.Id)).Lines.Single();
        Assert.Equal(4, line.OrderedQuantity);
        Assert.Equal(9m, line.UnitCostAmount);
    }

    [Fact]
    public async Task RemoveLine_ShouldRemoveTheLine_ThroughTheHandler()
    {
        // Arrange — a fresh context, as in production: the handler loads the order and its lines itself.
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host, (1, 10), (2, 4));
        var lineId = order.Lines[0].Id;
        PurchasingSeed.Detach(host);

        // Act
        var result = await new RemovePurchaseOrderLineCommandHandler(host.Context)
            .Handle(new RemovePurchaseOrderLineCommand(order.Id, lineId, Requester, false), Ct);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single((await ReloadAsync(host, order.Id)).Lines);
    }

    // Cancel / close

    [Fact]
    public async Task Cancel_ShouldApplyTheOwnerAndManagerRules_AndRecordTheActor()
    {
        using var host = new PurchasingTestHost();
        var draft = await PurchasingSeed.DraftAsync(host);
        var approved = await PurchasingSeed.ApprovedAsync(host);
        var handler = new CancelPurchaseOrderCommandHandler(host.Context, host.DateTime);
        var model = new CancelPurchaseOrderRequest { Reason = "no longer needed" };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new CancelPurchaseOrderCommand(draft.Id, model, "stranger", false), Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new CancelPurchaseOrderCommand(approved.Id, model, Requester, false), Ct));

        Assert.True((await handler.Handle(new CancelPurchaseOrderCommand(draft.Id, model, Requester, false), Ct)).IsSuccess);
        Assert.True((await handler.Handle(new CancelPurchaseOrderCommand(approved.Id, model, "mgr", true), Ct)).IsSuccess);

        var stored = await ReloadAsync(host, approved.Id);
        Assert.Equal(PurchaseOrderStatus.Cancelled, stored.Status);
        Assert.Equal("mgr", stored.CancelledBy);
    }

    [Fact]
    public async Task Cancel_OfAnApprovedOrder_ShouldConflict_WhileAReceiptIsInFlight()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        await PurchasingSeed.ReceivingAsync(host, order, null, (order.Lines[0].Id, 2));
        var handler = new CancelPurchaseOrderCommandHandler(host.Context, host.DateTime);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CancelPurchaseOrderCommand(order.Id, new CancelPurchaseOrderRequest { Reason = "r" }, "mgr", true), Ct));
    }

    [Fact]
    public async Task Close_ShouldWorkForAPartiallyReceivedOrder_ButNotWhileAReceiptIsInFlight()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        await PurchasingSeed.PostedReceiptAsync(host, order, (order.Lines[0].Id, 4));
        var handler = new ClosePurchaseOrderCommandHandler(host.Context, host.DateTime);
        var model = new ClosePurchaseOrderRequest { Reason = "supplier stopped" };
        var inFlight = await PurchasingSeed.ReceivingAsync(host, order, null, (order.Lines[0].Id, 2));

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new ClosePurchaseOrderCommand(order.Id, model, "mgr"), Ct));

        inFlight.Void("refused", host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(Ct);

        // A voided receipt no longer blocks the close.
        Assert.True((await handler.Handle(new ClosePurchaseOrderCommand(order.Id, model, "mgr"), Ct)).IsSuccess);
        var stored = await ReloadAsync(host, order.Id);
        Assert.Equal(PurchaseOrderStatus.Closed, stored.Status);
        Assert.Equal("mgr", stored.ClosedBy);
    }

    // Receive

    private static ReceivePurchaseOrderCommandHandler MakeReceive(
        PurchasingTestHost host,
        Mock<StarterKit.Inventory.Contracts.Services.IInventoryService> inventory) =>
        new(
            host.Context,
            inventory.Object,
            host.DateTime,
            NullLogger<ReceivePurchaseOrderCommandHandler>.Instance);

    private static ReceivePurchaseOrderCommand ReceiveCommand(
        long orderId,
        string? deliveryNote,
        params (long LineId, int Quantity)[] lines) =>
        new(
            orderId,
            new ReceivePurchaseOrderRequest
            {
                DeliveryNoteRef = deliveryNote,
                ReceivedAt = PurchasingBuilder.Now,
                Lines = lines.Select(x => new ReceivePurchaseOrderLineRequest { PurchaseOrderLineId = x.LineId, Quantity = x.Quantity }).ToList(),
            },
            "receiver");

    [Fact]
    public async Task Receive_ShouldRecordPostAndApply_ReturningTheReceiptId()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var inventory = InventoryMock.Create();
        PurchasingSeed.Detach(host);

        var result = await MakeReceive(host, inventory).Handle(ReceiveCommand(order.Id, "DN-1", (order.Lines[0].Id, 4)), Ct);

        Assert.True(result.IsSuccess);
        inventory.VerifyReceiveCalls(Times.Once());
        var receipt = await host.Context.GoodsReceipts.SingleAsync(Ct);
        Assert.Equal(receipt.Id, result.Data);
        Assert.Equal(GoodsReceiptStatus.Posted, receipt.Status);
        Assert.Equal(4, (await ReloadAsync(host, order.Id)).TotalReceivedQuantity);
    }

    [Fact]
    public async Task Receive_ShouldReturnNotFound_ForAnUnknownOrder()
    {
        using var host = new PurchasingTestHost();

        var result = await MakeReceive(host, InventoryMock.Create()).Handle(ReceiveCommand(999, null, (1, 1)), Ct);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Receive_ShouldReturnTheExistingReceipt_ForAReplayedDeliveryNote_WhateverTheLineOrder()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10), (2, 4));
        var (l1, l2) = (order.Lines[0].Id, order.Lines[1].Id);
        var inventory = InventoryMock.Create();
        var handler = MakeReceive(host, inventory);

        var first = await handler.Handle(ReceiveCommand(order.Id, "DN-1", (l1, 3), (l2, 1)), Ct);
        var second = await handler.Handle(ReceiveCommand(order.Id, " DN-1 ", (l2, 1), (l1, 3)), Ct);

        Assert.Equal(first.Data, second.Data);
        inventory.VerifyReceiveCalls(Times.Once());
        Assert.Equal(1, await host.Context.GoodsReceipts.CountAsync(Ct));
    }

    [Fact]
    public async Task Receive_ShouldConflict_WhenTheSameDeliveryNoteComesWithDifferentLines()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var lineId = order.Lines[0].Id;
        var handler = MakeReceive(host, InventoryMock.Create());
        await handler.Handle(ReceiveCommand(order.Id, "DN-1", (lineId, 3)), Ct);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(ReceiveCommand(order.Id, "DN-1", (lineId, 4)), Ct));

        Assert.Contains("different lines", ex.Message);
    }

    [Fact]
    public async Task Receive_ShouldFinishAReceiptStillPosting_ForTheSameDeliveryNote()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var receipt = await PurchasingSeed.ReceivingAsync(host, order, "DN-1", (order.Lines[0].Id, 3));
        var inventory = InventoryMock.Create();
        PurchasingSeed.Detach(host);

        var result = await MakeReceive(host, inventory).Handle(ReceiveCommand(order.Id, "DN-1", (order.Lines[0].Id, 3)), Ct);

        Assert.Equal(receipt.Id, result.Data);
        inventory.VerifyReceiveCalls(Times.Once());
        Assert.Equal(GoodsReceiptStatus.Posted, (await host.Context.GoodsReceipts.SingleAsync(Ct)).Status);
    }

    [Fact]
    public async Task Receive_ShouldNotTreatAVoidedReceiptAsAReplay()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var voided = await PurchasingSeed.ReceivingAsync(host, order, "DN-1", (order.Lines[0].Id, 3));
        voided.Void("refused", host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(Ct);
        PurchasingSeed.Detach(host);

        var result = await MakeReceive(host, InventoryMock.Create()).Handle(ReceiveCommand(order.Id, "DN-1", (order.Lines[0].Id, 3)), Ct);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(voided.Id, result.Data);
    }

    [Fact]
    public async Task Receive_ShouldRejectOverReceipt_IncludingQuantityStillInFlight()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var lineId = order.Lines[0].Id;
        await PurchasingSeed.ReceivingAsync(host, order, "DN-A", (lineId, 8));
        var inventory = InventoryMock.Create();
        PurchasingSeed.Detach(host);
        var handler = MakeReceive(host, inventory);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(ReceiveCommand(order.Id, "DN-B", (lineId, 3)), Ct));
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(ReceiveCommand(order.Id, null, (lineId, 11)), Ct));
        Assert.True((await handler.Handle(ReceiveCommand(order.Id, "DN-C", (lineId, 2)), Ct)).IsSuccess);
    }

    [Fact]
    public async Task Receive_ShouldRejectAReceivedDateBeforeApproval_AndAnUnreceivableOrder()
    {
        using var host = new PurchasingTestHost();
        var approved = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var draft = await PurchasingSeed.DraftAsync(host);
        var handler = MakeReceive(host, InventoryMock.Create());
        var early = ReceiveCommand(approved.Id, null, (approved.Lines[0].Id, 1));
        early = early with { Model = early.Model with { ReceivedAt = approved.ApprovedAt!.Value.AddMinutes(-5) } };

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(early, Ct));
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(ReceiveCommand(draft.Id, null, (draft.Lines[0].Id, 1)), Ct));
    }

    [Fact]
    public async Task Receive_ShouldVoidTheReceipt_AndRethrow_WhenInventoryRefusesAndNothingLanded()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ValidationException(new Dictionary<string, string[]> { ["l"] = ["gone"] }));

        await Assert.ThrowsAsync<ValidationException>(() =>
            MakeReceive(host, inventory).Handle(ReceiveCommand(order.Id, "DN-1", (order.Lines[0].Id, 3)), Ct));

        PurchasingSeed.Detach(host);
        Assert.Equal(GoodsReceiptStatus.Voided, (await host.Context.GoodsReceipts.SingleAsync(Ct)).Status);
        Assert.Equal(0, (await ReloadAsync(host, order.Id)).TotalReceivedQuantity);
    }

    [Fact]
    public async Task Receive_ShouldRetryAgainstFreshState_WhenADifferentDeliveryWinsTheTokenRace()
    {
        // Arrange — a competing delivery is recorded (rotating the order token) just before our first commit.
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var lineId = order.Lines[0].Id;
        var inventory = InventoryMock.Create();
        PurchasingSeed.Detach(host);

        host.SaveFaults.Arm(beforeSave: async () =>
        {
            using var other = host.NewContext();
            var competing = await other.PurchaseOrders.Include(x => x.Lines).SingleAsync(Ct);
            await other.GoodsReceipts.AddAsync(
                GoodsReceipt.Create(competing, "DN-OTHER", PurchasingBuilder.Now, "someone", [(lineId, 2)], new Dictionary<long, int>(), host.DateTime.UtcNow),
                Ct);
            await other.SaveChangesAsync(Ct);
        });

        // Act
        var result = await MakeReceive(host, inventory).Handle(ReceiveCommand(order.Id, "DN-MINE", (lineId, 3)), Ct);

        // Assert — both deliveries fit (2 + 3 <= 10), so the retry succeeds.
        Assert.True(result.IsSuccess);
        PurchasingSeed.Detach(host);
        Assert.Equal(2, await host.Context.GoodsReceipts.CountAsync(Ct));
        Assert.Equal(GoodsReceiptStatus.Posted, (await host.Context.GoodsReceipts.SingleAsync(x => x.DeliveryNoteRef == "DN-MINE", Ct)).Status);
    }

    [Fact]
    public async Task Receive_ShouldReValidateQuantities_WhenTheCompetingDeliveryLeavesNoRoom()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var lineId = order.Lines[0].Id;
        PurchasingSeed.Detach(host);

        host.SaveFaults.Arm(beforeSave: async () =>
        {
            using var other = host.NewContext();
            var competing = await other.PurchaseOrders.Include(x => x.Lines).SingleAsync(Ct);
            await other.GoodsReceipts.AddAsync(
                GoodsReceipt.Create(competing, "DN-OTHER", PurchasingBuilder.Now, "someone", [(lineId, 9)], new Dictionary<long, int>(), host.DateTime.UtcNow),
                Ct);
            await other.SaveChangesAsync(Ct);
        });

        await Assert.ThrowsAsync<ValidationException>(() =>
            MakeReceive(host, InventoryMock.Create()).Handle(ReceiveCommand(order.Id, "DN-MINE", (lineId, 3)), Ct));
    }

    [Fact]
    public async Task Receive_ShouldGiveUpWithAConcurrencyConflict_AfterThreeLostRaces()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var inventory = InventoryMock.Create();
        PurchasingSeed.Detach(host);
        host.SaveFaults.Arm(failTimes: 3);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            MakeReceive(host, inventory).Handle(ReceiveCommand(order.Id, null, (order.Lines[0].Id, 3)), Ct));

        Assert.Equal(PurchasingSaving.ConcurrencyMessage, ex.Message);
        inventory.VerifyReceiveCalls(Times.Never());
    }
}
