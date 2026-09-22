/** Mirrors Inventory.Contracts/Common/StockMovementReason.cs — serialized by name. */
export enum StockMovementReason {
  ManualAdjustment = "ManualAdjustment",
  OrderPlacement = "OrderPlacement",
  OrderCancellationRestore = "OrderCancellationRestore",
  PurchaseReceipt = "PurchaseReceipt",
  TransferIn = "TransferIn",
  TransferOut = "TransferOut",
  PurchaseReturnOut = "PurchaseReturnOut",
  CostRevaluation = "CostRevaluation",
}

/** Mirrors Inventory.Contracts/Stock/StockLevelDto.cs — `id`/`productId` are backend `long`, typed `string` here for consistency with every other opaque id in this app (e.g. `OrderLineDto.productId`). */
export interface StockLevelDto {
  id: string;
  productId: string;
  locationId: string;
  quantityOnHand: number;
  /** Base-currency moving-average unit cost; null unless the caller has `inventory.stock.view_cost`. */
  averageCostBase?: number | null;
  /** Base-currency total stock value; null unless the caller has `inventory.stock.view_cost`. */
  totalValueBase?: number | null;
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
  /** Base-currency unit cost of the movement; null unless the caller has `inventory.stock.view_cost`. */
  unitCostBase?: number | null;
  /** Signed base-currency value change of the movement; null unless the caller has `inventory.stock.view_cost`. */
  valueDeltaBase?: number | null;
}

/** Mirrors Inventory.Contracts/Stock/RecordStockMovementRequest.cs — no `reason` field, the backend always stamps `ManualAdjustment` for this endpoint. */
export interface RecordStockMovementRequest {
  productId: string;
  locationId: string;
  quantityDelta: number;
  /** Base-currency unit cost of an inbound movement. Only accepted with `inventory.stock.revalue`; omitted => current average, required when nothing is on hand. */
  unitCost?: number;
  note?: string;
}

/** Mirrors Inventory.Contracts/Stock/RevalueStockRequest.cs — `unitCost` must be > 0. */
export interface RevalueStockRequest {
  productId: string;
  locationId: string;
  unitCost: number;
  note?: string;
}

/** Mirrors Inventory.Contracts/Stock/StockValuationLineDto.cs — same `long` -> `string` id treatment as `StockLevelDto`. */
export interface StockValuationLineDto {
  productId: string;
  locationId: string;
  quantityOnHand: number;
  averageCostBase: number;
  totalValueBase: number;
}

/** Mirrors Inventory.Contracts/Stock/StockValuationDto.cs — NOT the standard `Paged<T>` shape: lines plus grand totals over every matching line (not just the page). */
export interface StockValuationDto {
  lines: StockValuationLineDto[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  grandTotalQuantity: number;
  grandTotalValueBase: number;
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
