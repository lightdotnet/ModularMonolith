using System.Reflection;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Contracts.Common;

namespace Transfers.Tests.TestSupport;

/// <summary>
/// Builds <see cref="StockTransfer"/> aggregates by driving their real state machine. Ids are normally
/// database-generated, so pure-domain tests assign them by reflection (the aggregate looks lines and
/// receipts up by id); handler tests persist through <see cref="TransfersTestHost"/> instead.
/// </summary>
internal static class TransferBuilder
{
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public const string Source = "loc-source";

    public const string Destination = "loc-dest";

    public static StockTransfer Draft(
        string source = Source,
        string destination = Destination,
        string? note = null) =>
        StockTransfer.Create(source, "Source", destination, "Destination", note, Now);

    /// <summary>Adds a line whose id is set to <paramref name="productId"/> so tests can address it.</summary>
    public static void AddLine(
        StockTransfer transfer,
        long productId = 1,
        int quantity = 10)
    {
        transfer.AddLine(productId, $"Product {productId}", $"SKU-{productId}", quantity);

        SetId(transfer.Lines[^1], productId);
    }

    public static StockTransfer DraftWithLines(params (long ProductId, int Quantity)[] lines)
    {
        var transfer = Draft();

        foreach (var (productId, quantity) in lines)
            AddLine(transfer, productId, quantity);

        return transfer;
    }

    public static StockTransfer Posting(params (long ProductId, int Quantity)[] lines)
    {
        var transfer = DraftWithLines(lines.Length == 0 ? [(1, 10)] : lines);

        SetId(transfer, 500);

        transfer.BeginDispatch(Now);

        return transfer;
    }

    /// <summary>Dispatched at a unit cost of 5 per line.</summary>
    public static StockTransfer Dispatched(params (long ProductId, int Quantity)[] lines)
    {
        var transfer = Posting(lines);

        transfer.CompleteDispatch(
            transfer.Lines.ToDictionary(x => x.Id, _ => 5m),
            Now);

        return transfer;
    }

    /// <summary>Begins a receipt and gives it an id (as the first commit would).</summary>
    public static TransferReceipt BeginReceipt(
        StockTransfer transfer,
        string clientRequestId,
        long receiptId,
        params (long LineId, int Quantity)[] lines)
    {
        var receipt = transfer.BeginReceive(clientRequestId, lines, Now);

        if (receipt.Id == 0)
            SetId(receipt, receiptId);

        return receipt;
    }

    public static TransferReceipt PostedReceipt(
        StockTransfer transfer,
        string clientRequestId,
        long receiptId,
        params (long LineId, int Quantity)[] lines)
    {
        var receipt = BeginReceipt(transfer, clientRequestId, receiptId, lines);

        transfer.CompleteReceive(receipt.Id, Now);

        return receipt;
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

    public static TransferStatus[] StatusesOtherThan(params TransferStatus[] allowed) =>
        Enum.GetValues<TransferStatus>().Except(allowed).ToArray();
}
