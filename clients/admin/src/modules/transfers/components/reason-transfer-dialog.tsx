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

interface ReasonTransferDialogProps {
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

/**
 * Shared confirm dialog for the two reason-bearing transitions (close, cancel) — both need a
 * required free-text reason (max 1000 chars) and a destructive confirm.
 */
export function ReasonTransferDialog({
  open,
  onOpenChange,
  title,
  description,
  reasonLabel,
  confirmLabel,
  successMessage,
  onSubmit,
  onDone,
}: ReasonTransferDialogProps) {
  const [submitting, run] = useGuardedAction();
  const [reason, setReason] = useState("");

  function handleConfirm() {
    const trimmed = reason.trim();
    if (!trimmed) return;

    run(
      () => onSubmit(trimmed),
      successMessage,
      () => {
        handleOpenChange(false);
        onDone();
      },
    );
  }

  // Any dismissal (Back, Escape, overlay) clears the reason so a stale one never reappears.
  function handleOpenChange(next: boolean) {
    if (!next) setReason("");
    onOpenChange(next);
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
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
          <Button disabled={submitting} onClick={() => handleOpenChange(false)} type="button" variant="outline">
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
