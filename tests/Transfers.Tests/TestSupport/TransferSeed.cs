using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace Transfers.Tests.TestSupport;

/// <summary>
/// Persists transfers through a real (Sqlite) <see cref="StarterKit.Transfers.Api.Data.TransfersDbContext"/>
/// by driving the aggregate's state machine, so ids are database-generated exactly as in production.
/// The host clock decides the audit <c>Created</c> time and is left untouched after seeding.
/// </summary>
internal static class TransferSeed
{
    public static async Task<StockTransfer> DraftAsync(
        TransfersTestHost host,
        params (long ProductId, int Quantity)[] lines)
    {
        var transfer = StockTransfer.Create(
            TransferBuilder.Source,
            "Source",
            TransferBuilder.Destination,
            "Destination",
            null,
            host.DateTime.UtcNow);

        foreach (var (productId, quantity) in lines.Length == 0 ? new (long ProductId, int Quantity)[] { (1L, 10) } : lines)
            transfer.AddLine(productId, $"Product {productId}", $"SKU-{productId}", quantity);

        await host.Context.StockTransfers.AddAsync(transfer, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return transfer;
    }

    /// <summary>A transfer stuck in Posting since <paramref name="postingStartedAt"/> (default: the host clock).</summary>
    public static async Task<StockTransfer> PostingAsync(
        TransfersTestHost host,
        DateTimeOffset? postingStartedAt = null,
        params (long ProductId, int Quantity)[] lines)
    {
        var transfer = await DraftAsync(host, lines);

        transfer.BeginDispatch(postingStartedAt ?? host.DateTime.UtcNow);

        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return transfer;
    }

    /// <summary>Dispatched at a unit cost of 5 per line.</summary>
    public static async Task<StockTransfer> DispatchedAsync(
        TransfersTestHost host,
        params (long ProductId, int Quantity)[] lines)
    {
        var transfer = await PostingAsync(host, null, lines);

        transfer.CompleteDispatch(
            transfer.Lines.ToDictionary(x => x.Id, _ => 5m),
            host.DateTime.UtcNow);

        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return transfer;
    }

    /// <summary>Records a Posting receipt (audit <c>Created</c> = the host clock at call time).</summary>
    public static async Task<TransferReceipt> ReceivingAsync(
        TransfersTestHost host,
        StockTransfer transfer,
        string clientRequestId,
        params (long LineId, int Quantity)[] lines)
    {
        var receipt = transfer.BeginReceive(clientRequestId, lines, host.DateTime.UtcNow);

        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return receipt;
    }

    /// <summary>Drops every tracked entity so the next query reads what was actually persisted.</summary>
    public static void Detach(TransfersTestHost host) => host.Context.ChangeTracker.Clear();
}
