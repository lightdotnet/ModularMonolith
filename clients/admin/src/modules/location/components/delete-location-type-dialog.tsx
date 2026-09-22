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
import { deleteLocationTypeAction } from "@/modules/location/api/delete-location-type-action";
import type { LocationTypeDto } from "@/modules/location/types/location-type";

interface DeleteLocationTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  locationType: LocationTypeDto | null;
  onDeleted: () => void;
}

export function DeleteLocationTypeDialog({
  open,
  onOpenChange,
  locationType,
  onDeleted,
}: DeleteLocationTypeDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!locationType) return;

    run(
      () => deleteLocationTypeAction(locationType.id),
      `"${locationType.name}" deleted.`,
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
          <DialogTitle>Delete location type</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{locationType?.name ?? "this type"}&quot;? All
            locations using this type must be reassigned first. This action cannot be undone.
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
