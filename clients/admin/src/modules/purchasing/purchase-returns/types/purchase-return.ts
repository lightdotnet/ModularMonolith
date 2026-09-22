import type { PurchaseReturnReason, PurchaseReturnStatus } from "@/modules/purchasing/common";

/**
 * Mirrors Purchasing.Contracts/PurchaseReturns/PurchaseReturnDto.cs (PurchaseReturnLineDto) — ids are backend
 * `long`, typed `string` here. `costRemovedBase` is null unless the caller has `inventory.stock.view_cost`.
 */
export interface PurchaseReturnLineDto {
  id: string;
  goodsReceiptLineId: string;
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  receiptUnitCostBase: number;
  costRemovedBase?: number | null;
  reason?: PurchaseReturnReason | null;
}

/** Mirrors Purchasing.Contracts/PurchaseReturns/PurchaseReturnDto.cs — `costRemovedBase` is null unless the caller has `inventory.stock.view_cost`. */
export interface PurchaseReturnDto {
  id: string;
  returnNumber: string;
  supplierId: string;
  supplierName: string;
  goodsReceiptId: string;
  receiptNumber: string;
  purchaseOrderId: string;
  locationId: string;
  locationName: string;
  reason: PurchaseReturnReason;
  note?: string | null;
  status: PurchaseReturnStatus;
  postedAt?: string | null;
  created: string;
  totalQuantity: number;
  expectedCreditBase?: number | null;
  costRemovedBase?: number | null;
  creditNoteNumber?: string | null;
  creditAmountBase?: number | null;
  creditedAt?: string | null;
  cancelledAt?: string | null;
  cancelledReason?: string | null;
  lines: PurchaseReturnLineDto[];
}

/** Mirrors Purchasing.Contracts/PurchaseReturns/CreatePurchaseReturnRequest.cs (PurchaseReturnLineRequest) — `goodsReceiptLineId` is written as a JSON number by the API layer. */
export interface PurchaseReturnLineRequest {
  goodsReceiptLineId: string;
  quantity: number;
  reason?: PurchaseReturnReason;
}

/** Mirrors Purchasing.Contracts/PurchaseReturns/CreatePurchaseReturnRequest.cs — `goodsReceiptId` is written as a JSON number by the API layer. */
export interface CreatePurchaseReturnRequest {
  goodsReceiptId: string;
  reason: PurchaseReturnReason;
  note?: string;
  lines: PurchaseReturnLineRequest[];
}

/** Mirrors Purchasing.Contracts/PurchaseReturns/UpdatePurchaseReturnRequest.cs. */
export interface UpdatePurchaseReturnRequest {
  reason: PurchaseReturnReason;
  note?: string;
  lines: PurchaseReturnLineRequest[];
}

/** Mirrors Purchasing.Contracts/PurchaseReturns/CancelPurchaseReturnRequest.cs. */
export interface CancelPurchaseReturnRequest {
  reason: string;
}

/** Mirrors Purchasing.Contracts/PurchaseReturns/MarkPurchaseReturnCreditedRequest.cs. */
export interface MarkPurchaseReturnCreditedRequest {
  creditNoteNumber: string;
  creditAmount: number;
}

/** Mirrors Purchasing.Contracts/PurchaseReturns/SearchPurchaseReturnRequest.cs. */
export interface PurchaseReturnSearchParams {
  status?: PurchaseReturnStatus;
  supplierId?: string;
  goodsReceiptId?: string;
  locationId?: string;
  /** Matches the return number exactly. */
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}

export const PURCHASE_RETURN_NOTE_MAX_LENGTH = 1000;
export const CREDIT_NOTE_NUMBER_MAX_LENGTH = 100;
