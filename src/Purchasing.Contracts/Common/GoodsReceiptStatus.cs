namespace StarterKit.Purchasing.Contracts.Common;

public enum GoodsReceiptStatus
{
    /// <summary>Recorded, but the inbound stock posting to Inventory has not been confirmed yet.</summary>
    Posting = 0,

    Posted = 1,

    /// <summary>
    /// Terminal: Inventory deterministically refused the inbound posting and nothing landed, so the
    /// receipt was abandoned. It counts toward no received or pending quantity.
    /// </summary>
    Voided = 2,
}
