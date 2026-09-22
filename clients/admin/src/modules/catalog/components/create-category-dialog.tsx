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
  createCategoryAction,
  type CreateCategoryFormState,
} from "@/modules/catalog/api/create-category-action";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";

const initialState: CreateCategoryFormState = {};

interface CreateCategoryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  parent: CategoryTreeNodeDto | null;
  onCreated: () => void;
}

export function CreateCategoryDialog({
  open,
  onOpenChange,
  parent,
  onCreated,
}: CreateCategoryDialogProps) {
  const [state, formAction, pending] = useActionState(createCategoryAction, initialState);

  useActionSuccessToast(state, "Category created.", () => {
    onCreated();
    onOpenChange(false);
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="parentCategoryId" value={parent?.id ?? ""} />

          <DialogHeader>
            <DialogTitle>
              Add category{parent ? ` under "${parent.name}"` : ""}
            </DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="cat-name">Name</Label>
            <Input id="cat-name" name="name" required />
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" loading={pending}>
              Create
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
