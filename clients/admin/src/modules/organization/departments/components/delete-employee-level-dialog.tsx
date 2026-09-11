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
import { deleteEmployeeLevelAction } from "@/modules/organization/departments/api/delete-employee-level-action";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

interface DeleteEmployeeLevelDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  level: EmployeeLevelDto | null;
  onDeleted: () => void;
}

export function DeleteEmployeeLevelDialog({
  open,
  onOpenChange,
  level,
  onDeleted,
}: DeleteEmployeeLevelDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!level) return;

    run(
      () => deleteEmployeeLevelAction(level.id),
      `Level "${level.name}" deleted.`,
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
          <DialogTitle>Delete employee level</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{level?.name ?? "this level"}&quot;? Employees
            currently on this level will have it cleared from their membership. This action cannot
            be undone.
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
