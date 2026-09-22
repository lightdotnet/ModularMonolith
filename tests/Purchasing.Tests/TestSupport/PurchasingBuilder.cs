using System.Reflection;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Purchasing.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;

namespace Purchasing.Tests.TestSupport;

/// <summary>
/// Builds Purchasing aggregates by driving their real state machines. Ids are normally
/// database-generated, so pure-domain tests assign them by reflection; handler tests persist through
/// <see cref="PurchasingTestHost"/> instead.
/// </summary>
internal static class PurchasingBuilder
{
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public const string Requester = "user-req";

    public const string Employee = "emp-req";

    public const string Location = "loc-1";

    public static Money Cost(decimal amount) => new(amount, CurrencyConstants.Default);

    public static PurchaseOrder Draft(string requester = Requester) =>
        PurchaseOrder.Create(1, "Supplier", Location, "Warehouse", null, requester, Employee, null, Now);

    /// <summary>Adds a line whose id is set to <paramref name="productId"/> so tests can address it.</summary>
    public static void AddLine(
        PurchaseOrder order,
        long productId = 1,
        int quantity = 10,
        decimal unitCost = 5m)
    {
        order.AddLine(productId, $"Product {productId}", $"SKU-{productId}", quantity, Cost(unitCost));

        SetId(order.Lines[^1], productId);
    }

    public static PurchaseOrder DraftWithLines(params (long ProductId, int Quantity)[] lines)
    {
        var order = Draft();

        SetId(order, 700);

        foreach (var (productId, quantity) in lines.Length == 0 ? new (long ProductId, int Quantity)[] { (1, 10) } : lines)
            AddLine(order, productId, quantity);

        return order;
    }

    public static PurchaseOrder Pending(
        string approvalRequestId = "wf-1",
        params (long ProductId, int Quantity)[] lines)
    {
        var order = DraftWithLines(lines);

        order.Submit(Requester, approvalRequestId, "emp-approver", "Approver", Now);

        return order;
    }

    public static PurchaseOrder Approved(params (long ProductId, int Quantity)[] lines)
    {
        var order = Pending("wf-1", lines);

        order.ApplyApprovalOutcome("wf-1", true, Now.AddMinutes(1));

        return order;
    }

    public static PurchaseOrder Rejected()
    {
        var order = Pending();

        order.ApplyApprovalOutcome("wf-1", false, Now.AddMinutes(1));

        return order;
    }

    public static GoodsReceipt Receipt(
        PurchaseOrder order,
        long receiptId,
        params (long LineId, int Quantity)[] lines)
    {
        var receipt = GoodsReceipt.Create(
            order,
            null,
            Now.AddMinutes(2),
            Requester,
            lines,
            new Dictionary<long, int>(),
            Now.AddMinutes(2));

        SetId(receipt, receiptId);

        for (var i = 0; i < receipt.Lines.Count; i++)
            SetId(receipt.Lines[i], receiptId * 10 + i + 1);

        return receipt;
    }

    public static GoodsReceipt PostedReceipt(
        PurchaseOrder order,
        long receiptId,
        params (long LineId, int Quantity)[] lines)
    {
        var receipt = Receipt(order, receiptId, lines);

        receipt.MarkPosted(Now.AddMinutes(3));

        return receipt;
    }

    public static PurchaseReturn Return(
        GoodsReceipt receipt,
        long returnId,
        params (long ReceiptLineId, int Quantity)[] lines)
    {
        var purchaseReturn = PurchaseReturn.Create(
            receipt,
            PurchaseReturnReason.Defective,
            null,
            lines.Select(x => (x.ReceiptLineId, x.Quantity, (PurchaseReturnReason?)null)).ToList(),
            new Dictionary<long, int>(),
            Now);

        SetId(purchaseReturn, returnId);

        for (var i = 0; i < purchaseReturn.Lines.Count; i++)
            SetId(purchaseReturn.Lines[i], returnId * 10 + i + 1);

        return purchaseReturn;
    }

    public static void SetId(
        object entity,
        long id)
    {
        for (var type = entity.GetType(); type is not null; type = type.BaseType)
        {
            var property = type.GetProperty(
                "Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (property?.GetSetMethod(true) is { } setter)
            {
                setter.Invoke(entity, [id]);
                return;
            }
        }

        throw new InvalidOperationException($"No settable Id on {entity.GetType().Name}.");
    }
}
