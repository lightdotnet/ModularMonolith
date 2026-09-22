/** Mirrors Transfers.Contracts/Common/TransferStatus.cs — serialized by name. */
export enum TransferStatus {
  Draft = "Draft",
  /** Short-lived: the stock issue is still being posted to Inventory. */
  Posting = "Posting",
  Dispatched = "Dispatched",
  PartiallyReceived = "PartiallyReceived",
  Received = "Received",
  Closed = "Closed",
  Cancelled = "Cancelled",
}

/** Mirrors Transfers.Contracts/Common/TransferReceiptStatus.cs — serialized by name. */
export enum TransferReceiptStatus {
  /** Recorded, but the inbound stock posting has not been confirmed yet. */
  Posting = "Posting",
  Posted = "Posted",
  /** Inventory refused the inbound posting; counts toward no received quantity. */
  Voided = "Voided",
}

/**
 * Mirrors Transfers.Contracts/StockTransfers/TransferLineDto.cs — `id`/`productId` are backend `long`,
 * typed `string` here for consistency with every other opaque id in this app. `unitCostBase` and
 * `closedShortValueBase` are null unless the caller has `inventory.stock.view_cost`.
 */
export interface TransferLineDto {
  id: string;
  productId: string;
  productName: string;
  sku: string;
  requestedQuantity: number;
  qtyDispatched: number;
  qtyReceived: number;
  qtyClosedShort: number;
  /** Dispatched, not yet received and not written off. */
  qtyInTransit: number;
  unitCostBase?: number | null;
  closedShortValueBase?: number | null;
}

/** Mirrors Transfers.Contracts/StockTransfers/TransferReceiptDto.cs (TransferReceiptLineDto). */
export interface TransferReceiptLineDto {
  id: string;
  transferLineId: string;
  quantity: number;
}

/** Mirrors Transfers.Contracts/StockTransfers/TransferReceiptDto.cs. */
export interface TransferReceiptDto {
  id: string;
  clientRequestId: string;
  status: TransferReceiptStatus;
  receivedAt: string;
  voidedAt?: string | null;
  voidReason?: string | null;
  lines: TransferReceiptLineDto[];
}

/** Mirrors Transfers.Contracts/StockTransfers/StockTransferDto.cs — `closedShortValueBase` is null unless the caller has `inventory.stock.view_cost`. */
export interface StockTransferDto {
  id: string;
  transferCode: string;
  sourceLocationId: string;
  sourceLocationName: string;
  destinationLocationId: string;
  destinationLocationName: string;
  status: TransferStatus;
  note?: string | null;
  requestedAt: string;
  dispatchedAt?: string | null;
  receivedAt?: string | null;
  closedAt?: string | null;
  closedReason?: string | null;
  cancelledAt?: string | null;
  cancelledReason?: string | null;
  totalRequestedQuantity: number;
  totalInTransitQuantity: number;
  closedShortValueBase?: number | null;
  lines: TransferLineDto[];
  receipts: TransferReceiptDto[];
}

/** Mirrors Transfers.Contracts/StockTransfers/CreateStockTransferRequest.cs. */
export interface CreateStockTransferRequest {
  sourceLocationId: string;
  destinationLocationId: string;
  note?: string;
}

/** Mirrors Transfers.Contracts/StockTransfers/UpdateStockTransferRequest.cs. */
export interface UpdateStockTransferRequest {
  sourceLocationId: string;
  destinationLocationId: string;
  note?: string;
}

/** Mirrors Transfers.Contracts/StockTransfers/AddStockTransferLineRequest.cs — `productId` is written as a JSON number by the API layer. */
export interface AddStockTransferLineRequest {
  productId: string;
  quantity: number;
}

/** Mirrors Transfers.Contracts/StockTransfers/UpdateStockTransferLineRequest.cs. */
export interface UpdateStockTransferLineRequest {
  quantity: number;
}

/** Mirrors Transfers.Contracts/StockTransfers/ReceiveStockTransferRequest.cs — `transferLineId` is written as a JSON number by the API layer. */
export interface ReceiveStockTransferRequest {
  clientRequestId: string;
  lines: { transferLineId: string; quantity: number }[];
}

/** Mirrors Transfers.Contracts/StockTransfers/CloseStockTransferRequest.cs. */
export interface CloseStockTransferRequest {
  reason: string;
}

/** Mirrors Transfers.Contracts/StockTransfers/CancelStockTransferRequest.cs. */
export interface CancelStockTransferRequest {
  reason: string;
}

/** Mirrors Transfers.Contracts/StockTransfers/SearchStockTransferRequest.cs. */
export interface StockTransferSearchParams {
  status?: TransferStatus;
  sourceLocationId?: string;
  destinationLocationId?: string;
  /** Matches the transfer code exactly. */
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Mirrors Transfers.Contracts/StockTransfers/StockTransferLimits.cs. */
export const MAX_TRANSFER_LINE_QUANTITY = 1_000_000;

/** Most lines the backend accepts in one receive request (StockTransferLimits.MaxLinesPerRequest). */
export const MAX_RECEIPT_LINES = 200;
