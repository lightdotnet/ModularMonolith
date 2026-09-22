import Link from "next/link";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { GoodsReceiptStatusBadge } from "@/modules/purchasing/common";
import type { PurchaseOrderReceiptSummaryDto } from "@/modules/purchasing/purchase-orders/types/purchase-order";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

interface PurchaseOrderReceiptsSectionProps {
  receipts: PurchaseOrderReceiptSummaryDto[];
  /** Links to the receipt detail only when the caller can view goods receipts. */
  canViewReceipts: boolean;
}

/** Goods receipts recorded against the order — a card list that works at any width. */
export function PurchaseOrderReceiptsSection({ receipts, canViewReceipts }: PurchaseOrderReceiptsSectionProps) {
  if (receipts.length === 0) {
    return <p className="text-sm text-muted-foreground">No receipts recorded.</p>;
  }

  return (
    <ul className="flex flex-col gap-3">
      {receipts.map((receipt) => (
        <li key={receipt.id} className="flex flex-col gap-2 rounded-lg border border-border p-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            {canViewReceipts ? (
              <Link
                href={`/purchasing/receipts/${receipt.id}`}
                className="font-medium text-primary hover:underline"
              >
                {receipt.receiptNumber}
              </Link>
            ) : (
              <span className="font-medium">{receipt.receiptNumber}</span>
            )}
            <GoodsReceiptStatusBadge status={receipt.status} />
          </div>
          <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 text-sm">
            <span className="text-muted-foreground">
              <LocalDateTime value={receipt.receivedAt} />
            </span>
            {receipt.deliveryNoteRef && (
              <span className="break-all text-muted-foreground">{`Delivery note ${receipt.deliveryNoteRef}`}</span>
            )}
            <span className="font-medium">{`${formatQuantity(receipt.totalQuantity)} units`}</span>
          </div>
        </li>
      ))}
    </ul>
  );
}
