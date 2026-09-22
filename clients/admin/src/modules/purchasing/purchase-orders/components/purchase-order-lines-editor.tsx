"use client";

import { useRouter } from "next/navigation";
import { MAX_LINES } from "@/modules/purchasing/common";
import { AddPurchaseOrderLineForm } from "@/modules/purchasing/purchase-orders/components/add-purchase-order-line-form";
import { PurchaseOrderLineRow } from "@/modules/purchasing/purchase-orders/components/purchase-order-line-row";
import type { PurchaseOrderLineDto } from "@/modules/purchasing/purchase-orders/types/purchase-order";

interface PurchaseOrderLinesEditorProps {
  purchaseOrderId: string;
  currency: string;
  lines: PurchaseOrderLineDto[];
}

/** Draft/Rejected line builder: the server page re-renders (revalidation + `router.refresh()`) after every mutation. */
export function PurchaseOrderLinesEditor({ purchaseOrderId, currency, lines }: PurchaseOrderLinesEditorProps) {
  const router = useRouter();
  const refresh = () => router.refresh();
  const atLimit = lines.length >= MAX_LINES;

  return (
    <div className="flex flex-col gap-4">
      {atLimit ? (
        <p className="text-sm text-muted-foreground">
          {`This order has the maximum of ${MAX_LINES} lines.`}
        </p>
      ) : (
        <AddPurchaseOrderLineForm purchaseOrderId={purchaseOrderId} refresh={refresh} />
      )}

      {lines.length === 0 ? (
        <p className="text-sm text-muted-foreground">No lines yet. Add a product to get started.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {lines.map((line) => (
            // Keyed by quantity and cost too, so a server-side correction remounts the row's local input state.
            <PurchaseOrderLineRow
              key={`${line.id}-${line.orderedQuantity}-${line.unitCost}`}
              purchaseOrderId={purchaseOrderId}
              currency={currency}
              line={line}
              refresh={refresh}
            />
          ))}
        </ul>
      )}
    </div>
  );
}
