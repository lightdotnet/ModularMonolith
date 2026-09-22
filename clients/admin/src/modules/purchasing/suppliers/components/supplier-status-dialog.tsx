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
import { SupplierStatus } from "@/modules/purchasing/common";
import { setSupplierStatusAction } from "@/modules/purchasing/suppliers/api/set-supplier-status-action";
import type { SupplierDto } from "@/modules/purchasing/suppliers/types/supplier";

interface SupplierStatusDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  supplier: SupplierDto | null;
}

export function SupplierStatusDialog({ open, onOpenChange, supplier }: SupplierStatusDialogProps) {
  const router = useRouter();
  const [submitting, run] = useGuardedAction();

  if (!supplier) return null;

  const activate = supplier.status !== SupplierStatus.Active;

  function handleConfirm() {
    if (!supplier) return;
    run(
      () => setSupplierStatusAction(supplier.id, activate),
      activate ? "Supplier activated." : "Supplier deactivated.",
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
            {activate ? "Activate" : "Deactivate"} {supplier.name}
          </DialogTitle>
          <DialogDescription>
            {activate
              ? "This supplier can be selected on new purchase orders again."
              : "This supplier can no longer be selected on new purchase orders. Existing orders are not affected."}
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
