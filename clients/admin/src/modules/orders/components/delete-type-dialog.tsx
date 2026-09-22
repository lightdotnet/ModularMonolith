"use client";

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
import { deleteTypeAction } from "@/modules/orders/api/delete-type-action";
import { OrderTypeCategory, type OrderTypeDto } from "@/modules/orders/types/order-type";

interface DeleteTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  type: OrderTypeDto | null;
  onDeleted: () => void;
}

export function DeleteTypeDialog({ open, onOpenChange, type, onDeleted }: DeleteTypeDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!type) return;

    run(
      () => deleteTypeAction(type.id, type.category),
      `"${type.name}" deleted.`,
      () => {
        onOpenChange(false);
        onDeleted();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Delete {type?.category === OrderTypeCategory.Fee ? "fee" : "payment"} type</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{type?.name ?? "this type"}&quot;? This action
            cannot be undone.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button disabled={deleting} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Cancel
          </Button>
          <Button
            disabled={deleting}
            loading={deleting}
            onClick={handleConfirm}
            type="button"
            variant="destructive"
          >
            Delete
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
