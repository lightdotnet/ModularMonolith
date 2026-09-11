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
  updateOrgUnitAction,
  type UpdateOrgUnitFormState,
} from "@/modules/organization/departments/api/update-org-unit-action";
import type { OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";

const updateInitialState: UpdateOrgUnitFormState = {};

interface EditOrgUnitDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  node: OrgUnitTreeNodeDto | null;
  onUpdated: () => void;
}

export function EditOrgUnitDialog({ open, onOpenChange, node, onUpdated }: EditOrgUnitDialogProps) {
  const [state, formAction, pending] = useActionState(updateOrgUnitAction, updateInitialState);

  useActionSuccessToast(state, `${node?.type ?? "Item"} updated.`, () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!node) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Edit {node.type.toLowerCase()}</DialogTitle>
        </DialogHeader>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={node.id} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="name">Name</Label>
            <Input id="name" name="name" defaultValue={node.name} required />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="code">Code</Label>
            <Input id="code" name="code" defaultValue={node.code} required />
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
