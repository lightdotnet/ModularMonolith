import { Badge } from "@/components/ui/badge";
import {
  GoodsReceiptStatus,
  PurchaseOrderStatus,
  PurchaseReturnReason,
  PurchaseReturnStatus,
  SupplierStatus,
} from "../types/enums";

type Variant = "default" | "outline" | "secondary" | "destructive";

const PO_VARIANT: Record<PurchaseOrderStatus, Variant> = {
  [PurchaseOrderStatus.Draft]: "outline",
  [PurchaseOrderStatus.PendingApproval]: "secondary",
  [PurchaseOrderStatus.Approved]: "default",
  [PurchaseOrderStatus.Rejected]: "destructive",
  [PurchaseOrderStatus.PartiallyReceived]: "secondary",
  [PurchaseOrderStatus.Received]: "default",
  [PurchaseOrderStatus.Closed]: "default",
  [PurchaseOrderStatus.Cancelled]: "outline",
};

const PO_LABEL: Record<PurchaseOrderStatus, string> = {
  [PurchaseOrderStatus.Draft]: "Draft",
  [PurchaseOrderStatus.PendingApproval]: "Pending approval",
  [PurchaseOrderStatus.Approved]: "Approved",
  [PurchaseOrderStatus.Rejected]: "Rejected",
  [PurchaseOrderStatus.PartiallyReceived]: "Partially received",
  [PurchaseOrderStatus.Received]: "Received",
  [PurchaseOrderStatus.Closed]: "Closed",
  [PurchaseOrderStatus.Cancelled]: "Cancelled",
};

export function formatPurchaseOrderStatus(status: PurchaseOrderStatus): string {
  return PO_LABEL[status] ?? status;
}

export function PurchaseOrderStatusBadge({ status }: { status: PurchaseOrderStatus }) {
  return <Badge variant={PO_VARIANT[status] ?? "outline"}>{formatPurchaseOrderStatus(status)}</Badge>;
}

const RECEIPT_VARIANT: Record<GoodsReceiptStatus, Variant> = {
  [GoodsReceiptStatus.Posting]: "secondary",
  [GoodsReceiptStatus.Posted]: "default",
  [GoodsReceiptStatus.Voided]: "destructive",
};

const RECEIPT_LABEL: Record<GoodsReceiptStatus, string> = {
  [GoodsReceiptStatus.Posting]: "Processing",
  [GoodsReceiptStatus.Posted]: "Posted",
  [GoodsReceiptStatus.Voided]: "Voided",
};

export function formatGoodsReceiptStatus(status: GoodsReceiptStatus): string {
  return RECEIPT_LABEL[status] ?? status;
}

export function GoodsReceiptStatusBadge({ status }: { status: GoodsReceiptStatus }) {
  return <Badge variant={RECEIPT_VARIANT[status] ?? "outline"}>{formatGoodsReceiptStatus(status)}</Badge>;
}

const RETURN_VARIANT: Record<PurchaseReturnStatus, Variant> = {
  [PurchaseReturnStatus.Draft]: "outline",
  [PurchaseReturnStatus.Posting]: "secondary",
  [PurchaseReturnStatus.Posted]: "default",
  [PurchaseReturnStatus.Credited]: "default",
  [PurchaseReturnStatus.Cancelled]: "outline",
};

const RETURN_LABEL: Record<PurchaseReturnStatus, string> = {
  [PurchaseReturnStatus.Draft]: "Draft",
  [PurchaseReturnStatus.Posting]: "Processing",
  [PurchaseReturnStatus.Posted]: "Posted",
  [PurchaseReturnStatus.Credited]: "Credited",
  [PurchaseReturnStatus.Cancelled]: "Cancelled",
};

export function formatPurchaseReturnStatus(status: PurchaseReturnStatus): string {
  return RETURN_LABEL[status] ?? status;
}

export function PurchaseReturnStatusBadge({ status }: { status: PurchaseReturnStatus }) {
  return <Badge variant={RETURN_VARIANT[status] ?? "outline"}>{formatPurchaseReturnStatus(status)}</Badge>;
}

const REASON_LABEL: Record<PurchaseReturnReason, string> = {
  [PurchaseReturnReason.Defective]: "Defective",
  [PurchaseReturnReason.Damaged]: "Damaged",
  [PurchaseReturnReason.ShortSupplied]: "Short supplied",
  [PurchaseReturnReason.WrongItem]: "Wrong item",
  [PurchaseReturnReason.Other]: "Other",
};

/** Blank for a missing reason. */
export function formatPurchaseReturnReason(reason: PurchaseReturnReason | null | undefined): string {
  return reason ? (REASON_LABEL[reason] ?? reason) : "";
}

export const PURCHASE_RETURN_REASON_OPTIONS = Object.values(PurchaseReturnReason).map((value) => ({
  value,
  label: REASON_LABEL[value],
}));

export function SupplierStatusBadge({ status }: { status: SupplierStatus }) {
  return (
    <Badge variant={status === SupplierStatus.Active ? "default" : "outline"}>
      {status === SupplierStatus.Active ? "Active" : "Inactive"}
    </Badge>
  );
}
