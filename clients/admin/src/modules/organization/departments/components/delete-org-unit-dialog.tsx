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
import { deleteOrgUnitAction } from "@/modules/organization/departments/api/delete-org-unit-action";
import type { OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";

interface DeleteOrgUnitDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  node: OrgUnitTreeNodeDto | null;
  onDeleted: () => void;
}

export function DeleteOrgUnitDialog({
  open,
  onOpenChange,
  node,
  onDeleted,
}: DeleteOrgUnitDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!node) return;

    run(
      () => deleteOrgUnitAction(node.id),
      `"${node.name}" deleted.`,
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
          <DialogTitle>Delete {node?.type.toLowerCase() ?? "item"}</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{node?.name ?? "this item"}&quot;? It must have
            no sub-departments/teams and no assigned employees. This action cannot be undone.
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
