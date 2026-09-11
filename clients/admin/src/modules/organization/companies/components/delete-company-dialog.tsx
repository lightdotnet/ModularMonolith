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
import { deleteCompanyAction } from "@/modules/organization/companies/api/delete-company-action";
import type { CompanyDto } from "@/modules/organization/companies/types/company";

interface DeleteCompanyDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  company: CompanyDto | null;
  onDeleted: () => void;
}

export function DeleteCompanyDialog({
  open,
  onOpenChange,
  company,
  onDeleted,
}: DeleteCompanyDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!company) return;

    run(
      () => deleteCompanyAction(company.id),
      `Company "${company.name}" deleted.`,
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
          <DialogTitle>Delete company</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{company?.name ?? "this company"}&quot;? This
            action cannot be undone. Companies with existing departments/teams or employees cannot
            be deleted.
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
