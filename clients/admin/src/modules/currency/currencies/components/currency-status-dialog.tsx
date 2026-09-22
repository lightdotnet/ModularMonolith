"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { setCurrencyStatusAction } from "@/modules/currency/currencies/api/set-currency-status-action";
import type { CurrencyDto } from "@/modules/currency/currencies/types/currency";

interface CurrencyStatusDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  currency: CurrencyDto | null;
}

export function CurrencyStatusDialog({ open, onOpenChange, currency }: CurrencyStatusDialogProps) {
  const router = useRouter();
  const [submitting, run] = useGuardedAction();

  if (!currency) return null;

  const activate = !currency.isActive;

  function handleConfirm() {
    if (!currency) return;
    run(
      () => setCurrencyStatusAction(currency.code, activate),
      activate ? "Currency activated." : "Currency deactivated.",
      () => {
        onOpenChange(false);
        router.refresh();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>
            {activate ? "Activate" : "Deactivate"} {currency.code}
          </DialogTitle>
          <DialogDescription>
            {activate
              ? "This currency can be used for product prices and new exchange rates again."
              : "This currency can no longer be used for product prices or new exchange rates. Existing products and orders are not changed."}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button disabled={submitting} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button
            loading={submitting}
            onClick={handleConfirm}
            type="button"
            variant={activate ? "default" : "destructive"}
          >
            {activate ? "Activate" : "Deactivate"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
