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
  updateEmployeeLevelAction,
  type UpdateEmployeeLevelFormState,
} from "@/modules/organization/departments/api/update-employee-level-action";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

const updateInitialState: UpdateEmployeeLevelFormState = {};

interface EditEmployeeLevelDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  level: EmployeeLevelDto | null;
  onUpdated: () => void;
}

export function EditEmployeeLevelDialog({
  open,
  onOpenChange,
  level,
  onUpdated,
}: EditEmployeeLevelDialogProps) {
  const [state, formAction, pending] = useActionState(updateEmployeeLevelAction, updateInitialState);

  useActionSuccessToast(state, "Level updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!level) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Edit employee level</DialogTitle>
        </DialogHeader>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={level.id} />
          <input type="hidden" name="companyId" value={level.companyId} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" defaultValue={level.name} required />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="code">Code</Label>
              <Input id="code" name="code" defaultValue={level.code} required />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="rank">Rank</Label>
            <Input id="rank" name="rank" type="number" defaultValue={level.rank} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="description">Description</Label>
            <Input id="description" name="description" defaultValue={level.description ?? ""} />
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
