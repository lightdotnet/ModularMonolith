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
import { cancelOrderAction } from "@/modules/orders/api/cancel-order-action";

interface CancelOrderDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  orderId: string;
  onCancelled: () => void;
}

export function CancelOrderDialog({ open, onOpenChange, orderId, onCancelled }: CancelOrderDialogProps) {
  const [cancelling, run] = useGuardedAction();
  const [reason, setReason] = useState("");

  function handleConfirm() {
    if (!reason.trim()) return;

    run(
      () => cancelOrderAction(orderId, reason.trim()),
      "Order cancelled.",
      () => {
        setReason("");
        onOpenChange(false);
        onCancelled();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Cancel order</DialogTitle>
          <DialogDescription>
            This action cannot be undone. Please provide a reason for cancelling this order.
          </DialogDescription>
        </DialogHeader>

        <Textarea
          aria-label="Cancellation reason"
          placeholder="Reason"
          maxLength={1000}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          required
        />

        <DialogFooter>
          <Button disabled={cancelling} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button
            disabled={cancelling || !reason.trim()}
            loading={cancelling}
            onClick={handleConfirm}
            type="button"
            variant="destructive"
          >
            Cancel order
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
