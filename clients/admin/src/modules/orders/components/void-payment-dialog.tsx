"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { voidPaymentAction } from "@/modules/orders/api/void-payment-action";

interface VoidPaymentDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  paymentId: string;
  onVoided: () => void;
}

export function VoidPaymentDialog({ open, onOpenChange, paymentId, onVoided }: VoidPaymentDialogProps) {
  const [voiding, run] = useGuardedAction();
  const [reason, setReason] = useState("");

  function handleConfirm() {
    if (!reason.trim()) return;

    run(
      () => voidPaymentAction(paymentId, reason.trim()),
      "Payment voided.",
      () => {
        setReason("");
        onOpenChange(false);
        onVoided();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Void payment</DialogTitle>
          <DialogDescription>
            This action cannot be undone. Please provide a reason for voiding this payment.
          </DialogDescription>
        </DialogHeader>

        <Textarea
          aria-label="Void reason"
          placeholder="Reason"
          maxLength={1000}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          required
        />

        <DialogFooter>
          <Button disabled={voiding} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button
            disabled={voiding || !reason.trim()}
            loading={voiding}
            onClick={handleConfirm}
            type="button"
            variant="destructive"
          >
            Void payment
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
