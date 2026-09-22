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
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { receiveTransferAction } from "@/modules/transfers/api/receive-transfer-action";
import { MAX_RECEIPT_LINES, type TransferLineDto } from "@/modules/transfers/types/transfer";
import { generateRequestId } from "@/modules/transfers/utils/request-id";

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

interface ReceiveTransferDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  transferId: string;
  transferCode: string;
  destinationLocationName: string;
  lines: TransferLineDto[];
  onReceived: () => void;
}

export function ReceiveTransferDialog({
  open,
  onOpenChange,
  transferId,
  transferCode,
  destinationLocationName,
  lines,
  onReceived,
}: ReceiveTransferDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="flex max-h-[90vh] flex-col gap-4 sm:max-w-xl"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Receive {transferCode}</DialogTitle>
          <DialogDescription>
            Enter the quantities that arrived at {destinationLocationName}. Each line cannot exceed
            what is still in transit; leave a line at 0 to skip it.
          </DialogDescription>
        </DialogHeader>
        {/* Mounted only while the dialog is open, so every open gets a fresh request id and
            prefilled quantities; retries within one open reuse the same id. */}
        <ReceiveTransferForm
          transferId={transferId}
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

function ReceiveTransferForm({
  transferId,
  lines,
  onCancel,
  onReceived,
}: {
  transferId: string;
  lines: TransferLineDto[];
  onCancel: () => void;
  onReceived: () => void;
}) {
  const receivable = lines.filter((line) => line.qtyInTransit > 0);
  // Stable across retries of an ambiguous failure (idempotent resubmit); regenerated after a
  // deterministic backend refusal, since that request id can never succeed again (e.g. Voided).
  const [clientRequestId, setClientRequestId] = useState(generateRequestId);
  const [quantities, setQuantities] = useState<Record<string, string>>(() =>
    Object.fromEntries(receivable.map((line) => [String(line.id), String(line.qtyInTransit)])),
  );
  const [submitting, run] = useGuardedAction();
  const [error, setError] = useState("");

  function parsed(line: TransferLineDto): number | null {
    const raw = (quantities[String(line.id)] ?? "").trim();
    if (raw === "") return 0;
    const value = Number(raw);
    if (!Number.isInteger(value) || value < 0 || value > line.qtyInTransit) return null;
    return value;
  }

  const tooManyLines = receivable.length > MAX_RECEIPT_LINES;
  const anyInvalid = receivable.some((line) => parsed(line) === null);
  const payload = receivable
    .map((line) => ({ transferLineId: String(line.id), quantity: parsed(line) ?? 0 }))
    .filter((line) => line.quantity > 0);

  function handleSubmit() {
    if (tooManyLines || anyInvalid || payload.length === 0) return;
    setError("");

    run(
      async () => {
        const result = await receiveTransferAction(transferId, clientRequestId, payload);
        if (result.error) {
          setError(result.error);
          if (result.definitive) setClientRequestId(generateRequestId());
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

        {tooManyLines && (
          <Alert variant="destructive">
            <AlertDescription>
              {`This transfer has ${receivable.length} lines still in transit, but a receipt can hold at most ${MAX_RECEIPT_LINES}. It cannot be received from this screen; close it or contact an administrator.`}
            </AlertDescription>
          </Alert>
        )}

        {!tooManyLines && receivable.map((line) => {
          const invalid = parsed(line) === null;
          return (
            <div
              key={line.id}
              className="flex flex-col gap-2 rounded-lg border border-border p-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="flex min-w-0 flex-col">
                <span className="truncate font-medium">{line.productName}</span>
                <span className="text-xs text-muted-foreground">
                  {line.sku} · In transit {formatQuantity(line.qtyInTransit)}
                </span>
              </div>
              <Input
                aria-label={`Received quantity for ${line.productName}`}
                aria-invalid={invalid || undefined}
                className="w-full sm:w-28"
                type="number"
                min="0"
                step="1"
                max={line.qtyInTransit}
                value={quantities[String(line.id)] ?? ""}
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
        <Button
          disabled={submitting || tooManyLines || anyInvalid || payload.length === 0}
          loading={submitting}
          onClick={handleSubmit}
          type="button"
        >
          Record receipt
        </Button>
      </DialogFooter>
    </>
  );
}
