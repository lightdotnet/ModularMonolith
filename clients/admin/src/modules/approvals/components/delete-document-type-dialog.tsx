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
import { deleteDocumentTypeAction } from "@/modules/approvals/api/delete-document-type-action";
import type { ApprovalDocumentTypeDto } from "@/modules/approvals/types/document-type";

interface DeleteDocumentTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  documentType: ApprovalDocumentTypeDto | null;
  onDeleted: () => void;
}

export function DeleteDocumentTypeDialog({
  open,
  onOpenChange,
  documentType,
  onDeleted,
}: DeleteDocumentTypeDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!documentType) return;

    run(
      () => deleteDocumentTypeAction(documentType.id),
      `Document type "${documentType.name}" deleted.`,
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
          <DialogTitle>Delete document type</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{documentType?.name ?? "this document type"}&quot;?
            The backend will reject this if the type is still used by any approval request. This
            action cannot be undone.
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
