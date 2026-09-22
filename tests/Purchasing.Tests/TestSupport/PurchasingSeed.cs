using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Purchasing.Api.Domain.Suppliers;
using StarterKit.Purchasing.Contracts.Common;

namespace Purchasing.Tests.TestSupport;

/// <summary>
/// Persists Purchasing aggregates through a real (Sqlite) <c>PurchasingDbContext</c> by driving their
/// state machines, so ids are database-generated exactly as in production. The host clock decides the
/// audit <c>Created</c> time and is left untouched after seeding.
/// </summary>
internal static class PurchasingSeed
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<Supplier> SupplierAsync(
        PurchasingTestHost host,
        string code = "SUP1")
    {
        var supplier = Supplier.Create(code, "Supplier", null, null, null, null, null);

        await host.Context.Suppliers.AddAsync(supplier, Ct);
        await host.Context.SaveChangesAsync(Ct);

        return supplier;
    }

    /// <summary>A persisted draft order (requester <see cref="PurchasingBuilder.Requester"/>) with the given lines at cost 5.</summary>
    public static Task<PurchaseOrder> EmptyDraftAsync(PurchasingTestHost host) => DraftAsync(host, true, []);

    public static Task<PurchaseOrder> DraftAsync(
        PurchasingTestHost host,
        params (long ProductId, int Quantity)[] lines) => DraftAsync(host, false, lines);

    private static async Task<PurchaseOrder> DraftAsync(
        PurchasingTestHost host,
        bool empty,
        (long ProductId, int Quantity)[] lines)
    {
        var supplier = await SupplierAsync(host, $"S{Guid.NewGuid():N}"[..10]);

        var order = PurchaseOrder.Create(
            supplier.Id,
            supplier.Name,
            PurchasingBuilder.Location,
            "Warehouse",
            null,
            PurchasingBuilder.Requester,
            PurchasingBuilder.Employee,
            null,
            host.DateTime.UtcNow);

        foreach (var (productId, quantity) in lines.Length == 0 && !empty ? new (long ProductId, int Quantity)[] { (1, 10) } : lines)
            order.AddLine(productId, $"Product {productId}", $"SKU-{productId}", quantity, PurchasingBuilder.Cost(5m));

        await host.Context.PurchaseOrders.AddAsync(order, Ct);
        await host.Context.SaveChangesAsync(Ct);

        return order;
    }

    public static async Task<PurchaseOrder> PendingAsync(
        PurchasingTestHost host,
        string approvalRequestId = "wf-1",
        params (long ProductId, int Quantity)[] lines)
    {
        var order = await DraftAsync(host, lines);

        order.Submit(PurchasingBuilder.Requester, approvalRequestId, "emp-approver", "Approver", host.DateTime.UtcNow);

        await host.Context.SaveChangesAsync(Ct);

        return order;
    }

    public static async Task<PurchaseOrder> ApprovedAsync(
        PurchasingTestHost host,
        params (long ProductId, int Quantity)[] lines)
    {
        var order = await PendingAsync(host, "wf-1", lines);

        order.ApplyApprovalOutcome("wf-1", true, host.DateTime.UtcNow.AddMinutes(-30));

        await host.Context.SaveChangesAsync(Ct);

        return order;
    }

    /// <summary>A Posting receipt whose audit <c>Created</c> is the host clock at call time.</summary>
    public static async Task<GoodsReceipt> ReceivingAsync(
        PurchasingTestHost host,
        PurchaseOrder order,
        string? deliveryNoteRef,
        params (long LineId, int Quantity)[] lines)
    {
        // A delivery cannot pre-date the approval, whatever the (possibly back-dated) audit clock says.
        var receivedAt = order.ApprovedAt is { } approvedAt && approvedAt > host.DateTime.UtcNow
            ? approvedAt
            : host.DateTime.UtcNow;

        var receipt = GoodsReceipt.Create(
            order,
            deliveryNoteRef,
            receivedAt,
            "receiver",
            lines,
            new Dictionary<long, int>(),
            host.DateTime.UtcNow);

        await host.Context.GoodsReceipts.AddAsync(receipt, Ct);
        await host.Context.SaveChangesAsync(Ct);

        return receipt;
    }

    /// <summary>A Posted receipt, with its quantities applied to the order.</summary>
    public static async Task<GoodsReceipt> PostedReceiptAsync(
        PurchasingTestHost host,
        PurchaseOrder order,
        params (long LineId, int Quantity)[] lines)
    {
        var receipt = await ReceivingAsync(host, order, null, lines);

        receipt.MarkPosted(host.DateTime.UtcNow);
        order.ApplyReceipt(lines, host.DateTime.UtcNow);

        await host.Context.SaveChangesAsync(Ct);

        return receipt;
    }

    public static async Task<PurchaseReturn> ReturnDraftAsync(
        PurchasingTestHost host,
        GoodsReceipt receipt,
        params (long ReceiptLineId, int Quantity)[] lines)
    {
        var purchaseReturn = PurchaseReturn.Create(
            receipt,
            PurchaseReturnReason.Defective,
            null,
            lines.Select(x => (x.ReceiptLineId, x.Quantity, (PurchaseReturnReason?)null)).ToList(),
            new Dictionary<long, int>(),
            host.DateTime.UtcNow);

        await host.Context.PurchaseReturns.AddAsync(purchaseReturn, Ct);
        await host.Context.SaveChangesAsync(Ct);

        return purchaseReturn;
    }

    public static async Task<PurchaseReturn> ReturnPostingAsync(
        PurchasingTestHost host,
        GoodsReceipt receipt,
        DateTimeOffset postingStartedAt,
        params (long ReceiptLineId, int Quantity)[] lines)
    {
        var purchaseReturn = await ReturnDraftAsync(host, receipt, lines);

        purchaseReturn.BeginPost("poster", postingStartedAt);

        await host.Context.SaveChangesAsync(Ct);

        return purchaseReturn;
    }

    public static void Detach(PurchasingTestHost host) => host.Context.ChangeTracker.Clear();
}
