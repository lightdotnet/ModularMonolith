namespace StarterKit.Purchasing.Contracts.Common;

public enum PurchaseReturnStatus
{
    /// <summary>Editable; nothing has left the location yet.</summary>
    Draft = 0,

    /// <summary>Internal, short-lived state while the stock issue is being posted to Inventory.</summary>
    Posting = 1,

    /// <summary>Stock has left the location.</summary>
    Posted = 2,

    /// <summary>The supplier's credit note was recorded against the return.</summary>
    Credited = 3,

    /// <summary>Abandoned while still a draft.</summary>
    Cancelled = 4,
}
