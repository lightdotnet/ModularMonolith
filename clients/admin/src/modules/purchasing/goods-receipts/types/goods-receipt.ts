import type { GoodsReceiptStatus } from "@/modules/purchasing/common";

/** Mirrors Purchasing.Contracts/GoodsReceipts/GoodsReceiptDto.cs (GoodsReceiptLineDto) — ids are backend `long`, typed `string` here. */
export interface GoodsReceiptLineDto {
  id: string;
  purchaseOrderLineId: string;
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  unitCostBase: number;
  lineTotalBase: number;
}

/** Mirrors Purchasing.Contracts/GoodsReceipts/GoodsReceiptDto.cs. */
export interface GoodsReceiptDto {
  id: string;
  receiptNumber: string;
  purchaseOrderId: string;
  poNumber: string;
  supplierId: string;
  supplierName: string;
  locationId: string;
  locationName: string;
  deliveryNoteRef?: string | null;
  receivedAt: string;
  status: GoodsReceiptStatus;
  stockPostedAt?: string | null;
  voidedAt?: string | null;
  voidReason?: string | null;
  totalQuantity: number;
  totalCostBase: number;
  lines: GoodsReceiptLineDto[];
}

/** Mirrors Purchasing.Contracts/GoodsReceipts/SearchGoodsReceiptRequest.cs. */
export interface GoodsReceiptSearchParams {
  status?: GoodsReceiptStatus;
  purchaseOrderId?: string;
  supplierId?: string;
  locationId?: string;
  /** Matches the receipt number exactly or a delivery note reference (contains). */
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}
