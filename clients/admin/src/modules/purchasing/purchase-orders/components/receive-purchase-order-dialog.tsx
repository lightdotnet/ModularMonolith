"use client";

import { useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { receivePurchaseOrderAction } from "@/modules/purchasing/purchase-orders/api/receive-purchase-order-action";
import {
  DELIVERY_NOTE_MAX_LENGTH,
  type PurchaseOrderLineDto,
} from "@/modules/purchasing/purchase-orders/types/purchase-order";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/** `yyyy-MM-ddTHH:mm` in the viewer's local time, for a datetime-local input. */
function nowLocalInputValue(): string {
  const now = new Date();
  const offsetMs = now.getTimezoneOffset() * 60_000;
  return new Date(now.getTime() - offsetMs).toISOString().slice(0, 16);
}

interface ReceivePurchaseOrderDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  purchaseOrderId: string;
  poNumber: string;
  locationName: string;
  lines: PurchaseOrderLineDto[];
  onReceived: () => void;
}

export function ReceivePurchaseOrderDialog({
  open,
  onOpenChange,
  purchaseOrderId,
  poNumber,
  locationName,
  lines,
  onReceived,
}: ReceivePurchaseOrderDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="flex max-h-[90vh] flex-col gap-4 sm:max-w-xl"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Receive {poNumber}</DialogTitle>
          <DialogDescription>
            Enter the quantities that arrived at {locationName}. A line cannot exceed what is still
            outstanding; leave a line at 0 to skip it.
          </DialogDescription>
        </DialogHeader>
        {/* Mounted only while open, so every open starts with prefilled outstanding quantities. */}
        <ReceivePurchaseOrderForm
          purchaseOrderId={purchaseOrderId}
          lines={lines}
          onCancel={() => onOpenChange(false)}
          onReceived={() => {
            onOpenChange(false);
            onReceived();
          }}
        />
      </DialogContent>
    </Dialog>
  );
}

function ReceivePurchaseOrderForm({
  purchaseOrderId,
  lines,
  onCancel,
  onReceived,
}: {
  purchaseOrderId: string;
  lines: PurchaseOrderLineDto[];
  onCancel: () => void;
  onReceived: () => void;
}) {
  const receivable = lines.filter((line) => line.outstandingQuantity > 0);
  const [quantities, setQuantities] = useState<Record<string, string>>(() =>
    Object.fromEntries(receivable.map((line) => [String(line.id), String(line.outstandingQuantity)])),
  );
  const [deliveryNote, setDeliveryNote] = useState("");
  const [receivedAt, setReceivedAt] = useState(nowLocalInputValue);
  // Captured once per open; the backend re-validates the future check on submit.
  const [openedAt] = useState(() => Date.now());
  const [submitting, run] = useGuardedAction();
  const [error, setError] = useState("");
  // After an ambiguous failure (timeout/network/5xx) the receipt may already be recorded: the delivery note and
  // quantities are locked so a retry sends exactly the same request, which the backend dedupes by delivery note.
  const [locked, setLocked] = useState(false);

  function parsed(line: PurchaseOrderLineDto): number | null {
    const raw = (quantities[String(line.id)] ?? "").trim();
    if (raw === "") return 0;
    const value = Number(raw);
    if (!Number.isInteger(value) || value < 0 || value > line.outstandingQuantity) return null;
    return value;
  }

  const receivedDate = new Date(receivedAt);
  const receivedAtInvalid = !receivedAt || Number.isNaN(receivedDate.getTime());
  const receivedAtFuture = !receivedAtInvalid && receivedDate.getTime() > openedAt + 60_000;
  const anyInvalid = receivable.some((line) => parsed(line) === null);
  const payload = receivable
    .map((line) => ({ purchaseOrderLineId: String(line.id), quantity: parsed(line) ?? 0 }))
    .filter((line) => line.quantity > 0);
  const noteInvalid = !deliveryNote.trim();
  const canSubmit = !noteInvalid && !anyInvalid && !receivedAtInvalid && !receivedAtFuture && payload.length > 0;

  function handleSubmit() {
    if (!canSubmit) return;
    setError("");

    run(
      async () => {
        const result = await receivePurchaseOrderAction(
          purchaseOrderId,
          deliveryNote,
          receivedDate.toISOString(),
          payload,
        );
        if (result.error) {
          setError(
            result.definitive === false
              ? `${result.error} The receipt may or may not have been recorded. Retry with the same delivery note and quantities: the delivery note prevents a double entry.`
              : result.error,
          );
          setLocked(result.definitive === false);
        }
        return result;
      },
      "Receipt recorded.",
      onReceived,
    );
  }

  return (
    <>
      <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="grid gap-3 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="receive-delivery-note">Delivery note</Label>
            <Input
              id="receive-delivery-note"
              maxLength={DELIVERY_NOTE_MAX_LENGTH}
              value={deliveryNote}
              readOnly={locked}
              required
              aria-describedby="receive-delivery-note-help"
              onChange={(event) => setDeliveryNote(event.target.value)}
            />
            <p id="receive-delivery-note-help" className="text-xs text-muted-foreground">
              Required. The delivery note number from the supplier identifies this delivery, so submitting it twice
              (for example after a timeout) is recognised instead of being recorded as a second receipt.
              Reusing a delivery note with different quantities is refused.
            </p>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="receive-received-at">Received at</Label>
            <Input
              id="receive-received-at"
              type="datetime-local"
              aria-invalid={receivedAtInvalid || receivedAtFuture || undefined}
              value={receivedAt}
              readOnly={locked}
              onChange={(event) => setReceivedAt(event.target.value)}
            />
            {receivedAtFuture && (
              <p className="text-xs text-destructive">The received time cannot be in the future.</p>
            )}
          </div>
        </div>

        {receivable.map((line) => {
          const invalid = parsed(line) === null;
          return (
            <div
              key={line.id}
              className="flex flex-col gap-2 rounded-lg border border-border p-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="flex min-w-0 flex-col">
                <span className="truncate font-medium">{line.productName}</span>
                <span className="text-xs text-muted-foreground">
                  {line.sku} · Outstanding {formatQuantity(line.outstandingQuantity)}
                </span>
              </div>
              <Input
                aria-label={`Received quantity for ${line.productName}`}
                aria-invalid={invalid || undefined}
                className="w-full sm:w-28"
                type="number"
                min="0"
                step="1"
                max={line.outstandingQuantity}
                value={quantities[String(line.id)] ?? ""}
                readOnly={locked}
                onChange={(event) =>
                  setQuantities((previous) => ({ ...previous, [String(line.id)]: event.target.value }))
                }
              />
            </div>
          );
        })}
      </div>

      <DialogFooter>
        <Button disabled={submitting} onClick={onCancel} type="button" variant="outline">
          Back
        </Button>
        <Button disabled={submitting || !canSubmit} loading={submitting} onClick={handleSubmit} type="button">
          Record receipt
        </Button>
      </DialogFooter>
    </>
  );
}
