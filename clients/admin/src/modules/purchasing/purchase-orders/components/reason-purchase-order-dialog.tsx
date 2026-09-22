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

interface ReasonPurchaseOrderDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  reasonLabel: string;
  confirmLabel: string;
  successMessage: string;
  /** Runs the mutation with the trimmed reason. */
  onSubmit: (reason: string) => Promise<{ error?: string; success?: boolean }>;
  onDone: () => void;
}

/** Shared confirm dialog for the reason-bearing transitions (close, cancel): required reason (max 1000 chars). */
export function ReasonPurchaseOrderDialog({
  open,
  onOpenChange,
  title,
  description,
  reasonLabel,
  confirmLabel,
  successMessage,
  onSubmit,
  onDone,
}: ReasonPurchaseOrderDialogProps) {
  const [submitting, run] = useGuardedAction();
  const [reason, setReason] = useState("");

  function handleConfirm() {
    const trimmed = reason.trim();
    if (!trimmed) return;

    run(
      () => onSubmit(trimmed),
      successMessage,
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
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <Textarea
          aria-label={reasonLabel}
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
            {confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
