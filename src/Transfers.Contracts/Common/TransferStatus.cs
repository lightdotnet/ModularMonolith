namespace StarterKit.Transfers.Contracts.Common;

public enum TransferStatus
{
    /// <summary>Editable; nothing has left the source location yet.</summary>
    Draft = 0,

    /// <summary>Internal, short-lived state while the stock issue is being posted to Inventory.</summary>
    Posting = 1,

    /// <summary>Stock has left the source location and is in transit.</summary>
    Dispatched = 2,

    PartiallyReceived = 3,

    Received = 4,

    /// <summary>The undelivered remainder was written off as a local variance.</summary>
    Closed = 5,

    /// <summary>Abandoned while still a draft.</summary>
    Cancelled = 6,
}
