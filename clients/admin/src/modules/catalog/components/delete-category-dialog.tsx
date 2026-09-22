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
import { deleteCategoryAction } from "@/modules/catalog/api/delete-category-action";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";

interface DeleteCategoryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  node: CategoryTreeNodeDto | null;
  onDeleted: () => void;
}

export function DeleteCategoryDialog({
  open,
  onOpenChange,
  node,
  onDeleted,
}: DeleteCategoryDialogProps) {
  const [deleting, run] = useGuardedAction();

  function handleConfirm() {
    if (!node) return;

    run(
      () => deleteCategoryAction(node.id),
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
          <DialogTitle>Delete category</DialogTitle>
          <DialogDescription>
            Are you sure you want to delete &quot;{node?.name ?? "this category"}&quot;? It must
            have no subcategories or products. This action cannot be undone.
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
