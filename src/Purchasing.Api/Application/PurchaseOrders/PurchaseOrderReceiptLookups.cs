using StarterKit.Purchasing.Api.Data;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders;

/// <summary>Read helpers over the goods receipts that are recorded but not yet posted for a purchase order.</summary>
internal static class PurchaseOrderReceiptLookups
{
    /// <summary>Quantity per purchase order line covered by receipts still <c>Posting</c> (recorded, not yet applied).</summary>
    public static async Task<IReadOnlyDictionary<long, int>> InFlightQuantitiesAsync(
        this PurchasingDbContext context,
        long purchaseOrderId,
        CancellationToken cancellationToken)
    {
        var rows = await context.GoodsReceiptLines
            .AsNoTracking()
            .Where(x => x.GoodsReceipt.PurchaseOrderId == purchaseOrderId
                && x.GoodsReceipt.Status == GoodsReceiptStatus.Posting)
            .GroupBy(x => x.PurchaseOrderLineId)
            .Select(g => new { LineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.LineId, x => x.Quantity);
    }

    public static Task<bool> HasReceiptInFlightAsync(
        this PurchasingDbContext context,
        long purchaseOrderId,
        CancellationToken cancellationToken) =>
        context.GoodsReceipts.AnyAsync(
            x => x.PurchaseOrderId == purchaseOrderId && x.Status == GoodsReceiptStatus.Posting,
            cancellationToken);
}
