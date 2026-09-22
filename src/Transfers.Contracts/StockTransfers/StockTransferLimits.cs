namespace StarterKit.Transfers.Contracts.StockTransfers;

/// <summary>Input bounds shared by the transfer request validators.</summary>
public static class StockTransferLimits
{
    /// <summary>Largest quantity accepted for a single transfer or receipt line.</summary>
    public const int MaxLineQuantity = 1_000_000;

    /// <summary>Most lines accepted in one receive request.</summary>
    public const int MaxLinesPerRequest = 200;
}
