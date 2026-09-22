"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect } from "@/components/ui/native-select";
import { Textarea } from "@/components/ui/textarea";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import {
  MAX_QUANTITY,
  PURCHASE_RETURN_REASON_OPTIONS,
  isValidQuantity,
} from "@/modules/purchasing/common";
import { createPurchaseReturnAction } from "@/modules/purchasing/purchase-returns/api/create-purchase-return-action";
import { updatePurchaseReturnAction } from "@/modules/purchasing/purchase-returns/api/update-purchase-return-action";
import { PURCHASE_RETURN_NOTE_MAX_LENGTH } from "@/modules/purchasing/purchase-returns/types/purchase-return";

/** A goods-receipt line the user may return, with any quantity/reason already on the draft. */
export interface ReturnLineCandidate {
  goodsReceiptLineId: string;
  productName: string;
  sku: string;
  /** Quantity received on the receipt line; undefined when the receipt could not be loaded (the backend still enforces the limit). */
  receiptQuantity?: number;
  /** Received minus what other non-cancelled returns already claim; undefined when that could not be determined. */
  returnableQuantity?: number;
  initialQuantity: number;
  initialReason: string;
}

interface PurchaseReturnFormProps {
  goodsReceiptId: string;
  candidates: ReturnLineCandidate[];
  initialReason: string;
  initialNote: string;
  /** Present when editing an existing Draft return; omit to create one. */
  returnId?: string;
}

function maxFor(line: ReturnLineCandidate): number {
  return line.returnableQuantity ?? line.receiptQuantity ?? MAX_QUANTITY;
}

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/**
 * Create/edit form for a Draft return. Every receipt line is listed with a quantity input (0 or empty =
 * not returned); the quantity already returned by other returns is enforced by the backend (409 shown).
 * Lines stack vertically on mobile.
 */
export function PurchaseReturnForm({
  goodsReceiptId,
  candidates,
  initialReason,
  initialNote,
  returnId,
}: PurchaseReturnFormProps) {
  const router = useRouter();
  const [submitting, run] = useGuardedAction();
  const [error, setError] = useState("");
  const [reason, setReason] = useState(initialReason);
  const [note, setNote] = useState(initialNote);
  const [quantities, setQuantities] = useState<Record<string, string>>(() =>
    Object.fromEntries(
      candidates.map((line) => [
        line.goodsReceiptLineId,
        line.initialQuantity > 0 ? String(line.initialQuantity) : "",
      ]),
    ),
  );
  const [lineReasons, setLineReasons] = useState<Record<string, string>>(() =>
    Object.fromEntries(candidates.map((line) => [line.goodsReceiptLineId, line.initialReason])),
  );

  function parsed(line: ReturnLineCandidate): number | null {
    const raw = (quantities[line.goodsReceiptLineId] ?? "").trim();
    if (raw === "") return 0;
    const value = Number(raw);
    if (!Number.isInteger(value) || value < 0 || value > maxFor(line)) return null;
    return value === 0 || isValidQuantity(value) ? value : null;
  }

  const anyInvalid = candidates.some((line) => parsed(line) === null);
  const payloadLines = candidates
    .map((line) => ({
      goodsReceiptLineId: line.goodsReceiptLineId,
      quantity: parsed(line) ?? 0,
      reason: lineReasons[line.goodsReceiptLineId] || undefined,
    }))
    .filter((line) => line.quantity > 0);
  const canSubmit = !anyInvalid && !!reason && payloadLines.length > 0;

  function handleSubmit() {
    if (!canSubmit) return;
    setError("");
    const input = { reason, note, lines: payloadLines };

    if (returnId) {
      run(
        async () => {
          const result = await updatePurchaseReturnAction(returnId, input);
          if (result.error) setError(result.error);
          return result;
        },
        "Purchase return updated.",
        () => router.refresh(),
      );
      return;
    }

    run(
      async () => {
        const result = await createPurchaseReturnAction(goodsReceiptId, input);
        if (result.error) setError(result.error);
        else if (result.returnId) router.push(`/purchasing/returns/${result.returnId}`);
        return result;
      },
      "Draft return created.",
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <NativeSelect
          id="return-reason"
          label="Reason"
          placeholder="Select a reason"
          options={PURCHASE_RETURN_REASON_OPTIONS}
          value={reason}
          onChange={setReason}
          required
        />
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label htmlFor="return-note">Note</Label>
          <Textarea
            id="return-note"
            maxLength={PURCHASE_RETURN_NOTE_MAX_LENGTH}
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <span className="text-sm font-medium">Lines to return</span>
        <p className="text-xs text-muted-foreground">
          Enter a quantity for each line you are returning; leave a line empty to keep it out of the
          return. {candidates.every((line) => line.returnableQuantity !== undefined)
            ? "The returnable quantity is what was received minus what other returns of this receipt already claim."
            : "The returnable quantity could not be determined for your access, so the backend rejects quantities above what was received minus what was already returned."}
        </p>
        <ul className="flex flex-col gap-2">
          {candidates.map((line) => {
            const invalid = parsed(line) === null;
            return (
              <li
                key={line.goodsReceiptLineId}
                className="flex flex-col gap-3 rounded-lg border border-border p-3 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="flex min-w-0 flex-col">
                  <span className="truncate font-medium">{line.productName}</span>
                  <span className="text-xs text-muted-foreground">
                    {[
                      line.sku,
                      line.receiptQuantity === undefined ? "" : `Received ${formatQuantity(line.receiptQuantity)}`,
                      line.returnableQuantity === undefined ? "" : `Returnable ${formatQuantity(line.returnableQuantity)}`,
                    ]
                      .filter(Boolean)
                      .join(" · ")}
                  </span>
                </div>
                <div className="grid grid-cols-2 gap-2 sm:flex sm:items-center">
                  <Input
                    aria-label={`Return quantity for ${line.productName}`}
                    aria-invalid={invalid || undefined}
                    className="w-full sm:w-24"
                    type="number"
                    min="0"
                    step="1"
                    max={maxFor(line)}
                    disabled={line.returnableQuantity === 0}
                    placeholder="Qty"
                    value={quantities[line.goodsReceiptLineId] ?? ""}
                    onChange={(event) =>
                      setQuantities((previous) => ({
                        ...previous,
                        [line.goodsReceiptLineId]: event.target.value,
                      }))
                    }
                  />
                  <NativeSelect
                    aria-label={`Reason for ${line.productName}`}
                    className="w-full sm:w-44"
                    placeholder="Same as return"
                    options={PURCHASE_RETURN_REASON_OPTIONS}
                    value={lineReasons[line.goodsReceiptLineId] ?? ""}
                    onChange={(value) =>
                      setLineReasons((previous) => ({ ...previous, [line.goodsReceiptLineId]: value }))
                    }
                  />
                </div>
              </li>
            );
          })}
        </ul>
      </div>

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button
          type="button"
          className="w-full sm:w-auto"
          disabled={!canSubmit}
          loading={submitting}
          onClick={handleSubmit}
        >
          {returnId ? "Save changes" : "Create draft return"}
        </Button>
      </div>
    </div>
  );
}
