"use client";

import { useState } from "react";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { removeTransferLineAction } from "@/modules/transfers/api/remove-transfer-line-action";
import { updateTransferLineAction } from "@/modules/transfers/api/update-transfer-line-action";
import { MAX_TRANSFER_LINE_QUANTITY, type TransferLineDto } from "@/modules/transfers/types/transfer";

interface TransferLineRowProps {
  transferId: string;
  line: TransferLineDto;
  refresh: () => void;
}

/** Editable Draft line — a stacked card layout that works on both desktop and mobile. */
export function TransferLineRow({ transferId, line, refresh }: TransferLineRowProps) {
  const [updating, runUpdate] = useGuardedAction();
  const [removing, runRemove] = useGuardedAction();
  const [quantity, setQuantity] = useState(String(line.requestedQuantity));

  function commitQuantity() {
    const next = Number(quantity);
    if (next === line.requestedQuantity) return;

    if (!Number.isInteger(next) || next < 1 || next > MAX_TRANSFER_LINE_QUANTITY) {
      setQuantity(String(line.requestedQuantity));
      return;
    }

    runUpdate(() => updateTransferLineAction(transferId, line.id, next), undefined, refresh);
  }

  function handleRemove() {
    runRemove(
      () => removeTransferLineAction(transferId, line.id),
      `"${line.productName}" removed.`,
      refresh,
    );
  }

  return (
    <li className="flex items-center justify-between gap-3 rounded-lg border border-border p-3">
      <div className="flex min-w-0 flex-col">
        <span className="truncate font-medium">{line.productName}</span>
        <span className="text-xs text-muted-foreground">{line.sku}</span>
      </div>

      <div className="flex shrink-0 items-center gap-2">
        <Input
          aria-label={`Quantity for ${line.productName}`}
          className="w-24"
          type="number"
          min="1"
          step="1"
          max={MAX_TRANSFER_LINE_QUANTITY}
          value={quantity}
          disabled={updating || removing}
          onChange={(event) => setQuantity(event.target.value)}
          onBlur={commitQuantity}
        />
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
    </li>
  );
}
