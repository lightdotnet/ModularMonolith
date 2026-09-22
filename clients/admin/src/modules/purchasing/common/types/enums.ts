/** Mirrors Purchasing.Contracts/Common/PurchaseOrderStatus.cs — serialized by name. */
export enum PurchaseOrderStatus {
  Draft = "Draft",
  PendingApproval = "PendingApproval",
  Approved = "Approved",
  Rejected = "Rejected",
  PartiallyReceived = "PartiallyReceived",
  Received = "Received",
  Closed = "Closed",
  Cancelled = "Cancelled",
}

/** Mirrors Purchasing.Contracts/Common/GoodsReceiptStatus.cs — serialized by name. */
export enum GoodsReceiptStatus {
  /** The stock movement is still being finalized. */
  Posting = "Posting",
  Posted = "Posted",
  Voided = "Voided",
}

/** Mirrors Purchasing.Contracts/Common/PurchaseReturnStatus.cs — serialized by name. */
export enum PurchaseReturnStatus {
  Draft = "Draft",
  /** The stock movement is still being finalized. */
  Posting = "Posting",
  Posted = "Posted",
  Credited = "Credited",
  Cancelled = "Cancelled",
}

/** Mirrors Purchasing.Contracts/Common/PurchaseReturnReason.cs — serialized by name. */
export enum PurchaseReturnReason {
  Defective = "Defective",
  Damaged = "Damaged",
  ShortSupplied = "ShortSupplied",
  WrongItem = "WrongItem",
  Other = "Other",
}

/** Mirrors Purchasing.Contracts/Common/SupplierStatus.cs — serialized by name. */
export enum SupplierStatus {
  Active = "Active",
  Inactive = "Inactive",
}
