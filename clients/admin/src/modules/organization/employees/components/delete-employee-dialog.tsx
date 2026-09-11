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
import { deleteEmployeeAction } from "@/modules/organization/employees/api/delete-employee-action";
import type { EmployeeDto } from "@/modules/organization/employees/types/employee";

interface DeleteEmployeeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  employee: EmployeeDto | null;
  onDeleted: () => void;
}

export function DeleteEmployeeDialog({
  open,
  onOpenChange,
  employee,
  onDeleted,
}: DeleteEmployeeDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!employee) return;

    run(
      () => deleteEmployeeAction(employee.id),
      `Employee "${employee.firstName} ${employee.lastName}" deleted.`,
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
          <DialogTitle>Delete employee</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{employee ? `${employee.firstName} ${employee.lastName}` : "this employee"}&quot;?
            This also removes their department/team memberships. Their login account (if any) is
            not deleted, only unlinked. This action cannot be undone.
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
