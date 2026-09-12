"use client";

import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  updateCategoryAction,
  type UpdateCategoryFormState,
} from "@/modules/catalog/api/update-category-action";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";

const initialState: UpdateCategoryFormState = {};

interface EditCategoryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  node: CategoryTreeNodeDto | null;
  onUpdated: () => void;
}

export function EditCategoryDialog({
  open,
  onOpenChange,
  node,
  onUpdated,
}: EditCategoryDialogProps) {
  const [state, formAction, pending] = useActionState(updateCategoryAction, initialState);

  useActionSuccessToast(state, "Category updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!node) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Edit category</DialogTitle>
        </DialogHeader>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={node.id} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-cat-name">Name</Label>
            <Input id="edit-cat-name" name="name" defaultValue={node.name} required />
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" loading={pending}>
              Save
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
