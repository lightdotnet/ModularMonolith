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
import { deleteRoleAction } from "@/modules/identity/roles/api/delete-role-action";
import type { RoleDto } from "@/modules/identity/roles/types/role";

interface DeleteRoleDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  role: RoleDto | null;
  onDeleted: () => void;
}

export function DeleteRoleDialog({
  open,
  onOpenChange,
  role,
  onDeleted,
}: DeleteRoleDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!role) return;

    run(
      () => deleteRoleAction(role.id),
      `Role "${role.name}" deleted.`,
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
          <DialogTitle>Delete role</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{role?.name ?? "this role"}
            &quot;? This action cannot be undone.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button
            disabled={deleting}
            onClick={() => onOpenChange(false)}
            type="button"
            variant="outline"
          >
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
