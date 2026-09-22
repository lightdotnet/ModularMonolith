using StarterKit.Purchasing.Api.Data;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns;

internal static class PurchaseReturnLookups
{
    /// <summary>
    /// Quantity per goods receipt line already claimed by the receipt's non-cancelled returns (drafts and
    /// posting/posted returns alike), optionally leaving out the return being edited.
    /// </summary>
    public static async Task<IReadOnlyDictionary<long, int>> AlreadyReturnedAsync(
        this PurchasingDbContext context,
        long goodsReceiptId,
        long? excludePurchaseReturnId,
        CancellationToken cancellationToken)
    {
        var rows = await context.PurchaseReturnLines
            .AsNoTracking()
            .Where(x => x.PurchaseReturn.GoodsReceiptId == goodsReceiptId
                && x.PurchaseReturn.Status != PurchaseReturnStatus.Cancelled
                && (excludePurchaseReturnId == null || x.PurchaseReturnId != excludePurchaseReturnId))
            .GroupBy(x => x.GoodsReceiptLineId)
            .Select(g => new { LineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.LineId, x => x.Quantity);
    }
}
