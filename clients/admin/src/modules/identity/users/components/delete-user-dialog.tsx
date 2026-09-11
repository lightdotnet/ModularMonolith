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
import { deleteUserAction } from "@/modules/identity/users/api/delete-user-action";
import type { UserDto } from "@/modules/identity/users/types/user";

interface DeleteUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: UserDto | null;
  onDeleted: () => void;
}

export function DeleteUserDialog({ open, onOpenChange, user, onDeleted }: DeleteUserDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!user) return;

    run(
      () => deleteUserAction(user.id),
      `User "${user.userName}" deleted.`,
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
          <DialogTitle>Delete user</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{user?.userName ?? "this user"}&quot;? This
            action cannot be undone.
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
