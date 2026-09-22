"use client";

import { useState } from "react";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { MAX_AMOUNT, MAX_QUANTITY, isValidAmountText, isValidQuantity } from "@/modules/purchasing/common";
import { removePurchaseOrderLineAction } from "@/modules/purchasing/purchase-orders/api/remove-purchase-order-line-action";
import { updatePurchaseOrderLineAction } from "@/modules/purchasing/purchase-orders/api/update-purchase-order-line-action";
import type { PurchaseOrderLineDto } from "@/modules/purchasing/purchase-orders/types/purchase-order";

function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return "";
  return new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

interface PurchaseOrderLineRowProps {
  purchaseOrderId: string;
  currency: string;
  line: PurchaseOrderLineDto;
  refresh: () => void;
}

/** Editable line — inputs stack vertically on mobile and sit in a single row from `sm` up. */
export function PurchaseOrderLineRow({ purchaseOrderId, currency, line, refresh }: PurchaseOrderLineRowProps) {
  const [updating, runUpdate] = useGuardedAction();
  const [removing, runRemove] = useGuardedAction();
  const [quantity, setQuantity] = useState(String(line.orderedQuantity));
  const [unitCost, setUnitCost] = useState(String(line.unitCost));

  const quantityInvalid = !isValidQuantity(Number(quantity));
  const costInvalid = !isValidAmountText(unitCost);

  function commit() {
    const nextQuantity = Number(quantity);
    // Invalid input is left in place with its validation message below, not silently reverted.
    if (quantityInvalid || costInvalid) return;

    const nextCost = Number(unitCost);
    if (nextQuantity === line.orderedQuantity && nextCost === line.unitCost) return;

    runUpdate(
      () => updatePurchaseOrderLineAction(purchaseOrderId, line.id, nextQuantity, nextCost),
      undefined,
      refresh,
    );
  }

  function handleRemove() {
    runRemove(
      () => removePurchaseOrderLineAction(purchaseOrderId, line.id),
      `"${line.productName}" removed.`,
      refresh,
    );
  }

  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border p-3">
      <div className="flex items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col">
          <span className="truncate font-medium">{line.productName}</span>
          <span className="text-xs text-muted-foreground">{line.sku}</span>
        </div>
        <div className="flex shrink-0 justify-end">
          <Button
            aria-label={`Remove ${line.productName}`}
            disabled={removing}
            onClick={handleRemove}
            size="icon"
            type="button"
            variant="outline"
          >
            <Trash2 />
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-2 items-end gap-3 sm:grid-cols-3">
        <div className="flex flex-col gap-1">
          <Label className="text-xs text-muted-foreground" htmlFor={`qty-${line.id}`}>
            Quantity
          </Label>
          <Input
            id={`qty-${line.id}`}
            aria-label={`Quantity for ${line.productName}`}
            aria-invalid={quantityInvalid || undefined}
            type="number"
            min="1"
            step="1"
            max={MAX_QUANTITY}
            value={quantity}
            disabled={updating || removing}
            onChange={(event) => setQuantity(event.target.value)}
            onBlur={commit}
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label className="text-xs text-muted-foreground" htmlFor={`cost-${line.id}`}>
            Unit cost
          </Label>
          <Input
            id={`cost-${line.id}`}
            aria-label={`Unit cost for ${line.productName}`}
            aria-invalid={costInvalid || undefined}
            type="number"
            min="0"
            step="any"
            max={MAX_AMOUNT}
            value={unitCost}
            disabled={updating || removing}
            onChange={(event) => setUnitCost(event.target.value)}
            onBlur={commit}
          />
        </div>
        <div className="col-span-2 flex flex-col gap-1 sm:col-span-1 sm:items-end">
          <span className="text-xs text-muted-foreground">Line total</span>
          <span className="text-sm font-medium">{`${currency} ${formatMoney(line.lineTotal)}`}</span>
        </div>
      </div>
      {(quantityInvalid || costInvalid) && (
        <p role="alert" className="text-xs text-destructive">
          {quantityInvalid
            ? "Quantity must be a whole number between 1 and 1,000,000."
            : "Unit cost must be between 0 and 1,000,000,000 with at most 4 decimals."}{" "}
          The line is saved once the value is valid.
        </p>
      )}
    </li>
  );
}
