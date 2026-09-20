/** Mirrors Inventory.Contracts/Common/StockMovementReason.cs — serialized by name. */
export enum StockMovementReason {
  ManualAdjustment = "ManualAdjustment",
  OrderPlacement = "OrderPlacement",
  OrderCancellationRestore = "OrderCancellationRestore",
  PurchaseReceipt = "PurchaseReceipt",
  TransferIn = "TransferIn",
  TransferOut = "TransferOut",
}

/** Mirrors Inventory.Contracts/Stock/StockLevelDto.cs — `id`/`productId` are backend `long`, typed `string` here for consistency with every other opaque id in this app (e.g. `OrderLineDto.productId`). */
export interface StockLevelDto {
  id: string;
  productId: string;
  locationId: string;
  quantityOnHand: number;
}

/** Mirrors Inventory.Contracts/Stock/StockAdjustmentDto.cs — same `long` -> `string` id treatment as `StockLevelDto`. */
export interface StockAdjustmentDto {
  id: string;
  productId: string;
  locationId: string;
  quantityDelta: number;
  reason: StockMovementReason;
  note?: string | null;
  occurredAt: string;
  performedByUserId: string;
  sourceOrderId?: string | null;
  sourceOrderLineId?: string | null;
  reversesAdjustmentId?: string | null;
}

/** Mirrors Inventory.Contracts/Stock/RecordStockMovementRequest.cs — no `reason` field, the backend always stamps `ManualAdjustment` for this endpoint. */
export interface RecordStockMovementRequest {
  productId: string;
  locationId: string;
  quantityDelta: number;
  note?: string;
}

/** Mirrors Inventory.Contracts/Stock/ProductStockTotalDto.cs — same `long` -> `string` id treatment as `StockLevelDto`. */
export interface ProductStockTotalDto {
  productId: string;
  totalQuantityOnHand: number;
  locationCount: number;
}

/** Mirrors Inventory.Contracts/Stock/SearchStockLevelRequest.cs */
export interface StockLevelSearchParams {
  productId?: string;
  locationId?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Mirrors Inventory.Contracts/Stock/SearchStockAdjustmentRequest.cs */
export interface StockAdjustmentSearchParams {
  productId?: string;
  locationId?: string;
  sourceOrderId?: string;
  pageNumber?: number;
  pageSize?: number;
}
