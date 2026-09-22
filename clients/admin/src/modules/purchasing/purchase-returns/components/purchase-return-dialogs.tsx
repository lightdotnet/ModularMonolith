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
import { Textarea } from "@/components/ui/textarea";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { MAX_AMOUNT, isValidAmountText } from "@/modules/purchasing/common";
import { cancelPurchaseReturnAction } from "@/modules/purchasing/purchase-returns/api/cancel-purchase-return-action";
import { creditPurchaseReturnAction } from "@/modules/purchasing/purchase-returns/api/credit-purchase-return-action";
import { postPurchaseReturnAction } from "@/modules/purchasing/purchase-returns/api/post-purchase-return-action";
import { CREDIT_NOTE_NUMBER_MAX_LENGTH } from "@/modules/purchasing/purchase-returns/types/purchase-return";

interface BaseDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  returnId: string;
  returnNumber: string;
  onDone: () => void;
}

/** Confirms posting; explains that stock leaves the location and that insufficient stock is refused by the backend. */
export function PostPurchaseReturnDialog({
  open,
  onOpenChange,
  returnId,
  returnNumber,
  locationName,
  purchaseOrderId,
  onDone,
}: BaseDialogProps & { locationName: string; purchaseOrderId?: string }) {
  const [submitting, run] = useGuardedAction();
  const [error, setError] = useState("");

  function handleConfirm() {
    setError("");
    run(
      async () => {
        const result = await postPurchaseReturnAction(returnId, purchaseOrderId);
        if (result.error) setError(result.error);
        return result;
      },
      "Purchase return posted.",
      () => {
        onOpenChange(false);
        onDone();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Post {returnNumber}</DialogTitle>
          <DialogDescription>
            The returned quantities leave stock at {locationName}. If there is not enough stock on hand the
            return cannot be posted. Once posted the return can no longer be edited or cancelled.
          </DialogDescription>
        </DialogHeader>
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <DialogFooter>
          <Button disabled={submitting} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button loading={submitting} onClick={handleConfirm} type="button">
            Post return
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** Cancels a Draft return with a required reason. */
export function CancelPurchaseReturnDialog({ open, onOpenChange, returnId, returnNumber, onDone }: BaseDialogProps) {
  const [submitting, run] = useGuardedAction();
  const [reason, setReason] = useState("");
  const [error, setError] = useState("");

  function handleConfirm() {
    const trimmed = reason.trim();
    if (!trimmed) return;
    setError("");

    run(
      async () => {
        const result = await cancelPurchaseReturnAction(returnId, trimmed);
        if (result.error) setError(result.error);
        return result;
      },
      "Purchase return cancelled.",
      () => {
        setReason("");
        onOpenChange(false);
        onDone();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Cancel {returnNumber}</DialogTitle>
          <DialogDescription>
            This draft return will be abandoned; no stock has moved. This cannot be undone. A reason is
            required.
          </DialogDescription>
        </DialogHeader>
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <Textarea
          aria-label="Cancellation reason"
          placeholder="Reason"
          maxLength={1000}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          required
        />
        <DialogFooter>
          <Button disabled={submitting} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button
            disabled={submitting || !reason.trim()}
            loading={submitting}
            onClick={handleConfirm}
            type="button"
            variant="destructive"
          >
            Cancel return
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** Records the supplier's credit note (informational bookkeeping) against a Posted return. */
export function CreditPurchaseReturnDialog({
  open,
  onOpenChange,
  returnId,
  returnNumber,
  initialAmount,
  onDone,
}: BaseDialogProps & { initialAmount?: number | null }) {
  const [submitting, run] = useGuardedAction();
  const [creditNoteNumber, setCreditNoteNumber] = useState("");
  const [creditAmount, setCreditAmount] = useState(
    initialAmount === null || initialAmount === undefined ? "" : String(initialAmount),
  );
  const [error, setError] = useState("");

  const amountValid = isValidAmountText(creditAmount);
  const canSubmit = !!creditNoteNumber.trim() && amountValid;

  function handleConfirm() {
    if (!canSubmit) return;
    setError("");

    run(
      async () => {
        const result = await creditPurchaseReturnAction(returnId, creditNoteNumber, creditAmount);
        if (result.error) setError(result.error);
        return result;
      },
      "Purchase return marked credited.",
      () => {
        onOpenChange(false);
        onDone();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Mark {returnNumber} credited</DialogTitle>
          <DialogDescription>
            Record the credit note received from the supplier. This is bookkeeping only; stock and cost are
            not changed.
          </DialogDescription>
        </DialogHeader>
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="credit-note-number">Credit note number</Label>
          <Input
            id="credit-note-number"
            maxLength={CREDIT_NOTE_NUMBER_MAX_LENGTH}
            value={creditNoteNumber}
            onChange={(event) => setCreditNoteNumber(event.target.value)}
            required
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="credit-amount">Credit amount</Label>
          <Input
            id="credit-amount"
            type="number"
            min="0"
            step="any"
            max={MAX_AMOUNT}
            aria-invalid={(creditAmount !== "" && !amountValid) || undefined}
            value={creditAmount}
            onChange={(event) => setCreditAmount(event.target.value)}
            required
          />
        </div>
        <DialogFooter>
          <Button disabled={submitting} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button disabled={!canSubmit} loading={submitting} onClick={handleConfirm} type="button">
            Mark credited
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
