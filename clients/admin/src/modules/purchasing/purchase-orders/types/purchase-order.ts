import type { GoodsReceiptStatus, PurchaseOrderStatus } from "@/modules/purchasing/common";

/**
 * Mirrors Purchasing.Contracts/PurchaseOrders/PurchaseOrderDto.cs (PurchaseOrderLineDto) — `id`/`productId`
 * are backend `long`, typed `string` here like every other opaque id in this app.
 */
export interface PurchaseOrderLineDto {
  id: string;
  productId: string;
  productName: string;
  sku: string;
  orderedQuantity: number;
  receivedQuantity: number;
  returnedQuantity: number;
  outstandingQuantity: number;
  unitCost: number;
  lineTotal: number;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/PurchaseOrderDto.cs (PurchaseOrderReceiptSummaryDto). */
export interface PurchaseOrderReceiptSummaryDto {
  id: string;
  receiptNumber: string;
  status: GoodsReceiptStatus;
  deliveryNoteRef?: string | null;
  receivedAt: string;
  totalQuantity: number;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/PurchaseOrderDto.cs. */
export interface PurchaseOrderDto {
  id: string;
  poNumber: string;
  supplierId: string;
  supplierName: string;
  locationId: string;
  locationName: string;
  expectedAt?: string | null;
  status: PurchaseOrderStatus;
  note?: string | null;
  requesterEmployeeId: string;
  approvalRequestId?: string | null;
  approverEmployeeId?: string | null;
  approverName?: string | null;
  submittedAt?: string | null;
  approvedAt?: string | null;
  rejectedAt?: string | null;
  receivedAt?: string | null;
  closedAt?: string | null;
  closedReason?: string | null;
  cancelledAt?: string | null;
  cancelledReason?: string | null;
  created: string;
  currency: string;
  totalAmount: number;
  totalOrderedQuantity: number;
  totalReceivedQuantity: number;
  totalOutstandingQuantity: number;
  lines: PurchaseOrderLineDto[];
  receipts: PurchaseOrderReceiptSummaryDto[];
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/ApproverCandidateDto.cs. */
export interface PurchaseOrderApproverDto {
  employeeId: string;
  userId: string;
  name: string;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/CreatePurchaseOrderRequest.cs. */
export interface CreatePurchaseOrderRequest {
  supplierId: string;
  locationId: string;
  expectedAt?: string;
  note?: string;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/UpdatePurchaseOrderRequest.cs. */
export type UpdatePurchaseOrderRequest = CreatePurchaseOrderRequest;

/** Mirrors Purchasing.Contracts/PurchaseOrders/AddPurchaseOrderLineRequest.cs — `productId` is written as a JSON number by the API layer. */
export interface AddPurchaseOrderLineRequest {
  productId: string;
  quantity: number;
  unitCost: number;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/UpdatePurchaseOrderLineRequest.cs. */
export interface UpdatePurchaseOrderLineRequest {
  quantity: number;
  unitCost: number;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/SubmitPurchaseOrderRequest.cs. */
export interface SubmitPurchaseOrderRequest {
  approverEmployeeId: string;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/ReceivePurchaseOrderRequest.cs — `purchaseOrderLineId` is written as a JSON number by the API layer. */
export interface ReceivePurchaseOrderRequest {
  /** Required by this UI: the backend dedupes a receipt by delivery note reference. */
  deliveryNoteRef: string;
  receivedAt: string;
  lines: { purchaseOrderLineId: string; quantity: number }[];
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/ClosePurchaseOrderRequest.cs. */
export interface ClosePurchaseOrderRequest {
  reason: string;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/CancelPurchaseOrderRequest.cs. */
export interface CancelPurchaseOrderRequest {
  reason: string;
}

/** Mirrors Purchasing.Contracts/PurchaseOrders/SearchPurchaseOrderRequest.cs. */
export interface PurchaseOrderSearchParams {
  status?: PurchaseOrderStatus;
  supplierId?: string;
  locationId?: string;
  /** Matches the PO number exactly. */
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}

export const PURCHASE_ORDER_NOTE_MAX_LENGTH = 1000;
export const DELIVERY_NOTE_MAX_LENGTH = 100;

/** Slim supplier option handed from server pages to client pickers/filters. */
export interface SupplierOption {
  id: string;
  name: string;
  code: string;
  active: boolean;
}
