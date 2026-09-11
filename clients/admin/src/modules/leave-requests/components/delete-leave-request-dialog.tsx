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
import { deleteLeaveRequestAction } from "@/modules/leave-requests/api/delete-leave-request-action";
import type { LeaveRequestDto } from "@/modules/leave-requests/types/leave-request";

interface DeleteLeaveRequestDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  leaveRequest: LeaveRequestDto | null;
  onDeleted: () => void;
}

export function DeleteLeaveRequestDialog({
  open,
  onOpenChange,
  leaveRequest,
  onDeleted,
}: DeleteLeaveRequestDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!leaveRequest) return;

    run(
      () => deleteLeaveRequestAction(leaveRequest.id),
      "Leave request deleted.",
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
          <DialogTitle>Delete leave request</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete this {leaveRequest?.leaveType.toLowerCase()} leave
            request? If it&apos;s still pending, the related approval request will be cancelled.
            This action cannot be undone.
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
