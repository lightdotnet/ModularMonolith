"use client";

import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
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
import { NativeSelect } from "@/components/ui/native-select";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  updateLocationTypeAction,
  type UpdateLocationTypeFormState,
} from "@/modules/location/api/update-location-type-action";
import { LocationTypeStatus, type LocationTypeDto } from "@/modules/location/types/location-type";

const initialState: UpdateLocationTypeFormState = {};

interface EditLocationTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  locationType: LocationTypeDto | null;
  existingTypes: LocationTypeDto[];
  onUpdated: () => void;
}

export function EditLocationTypeDialog({
  open,
  onOpenChange,
  locationType,
  existingTypes,
  onUpdated,
}: EditLocationTypeDialogProps) {
  const [state, formAction, pending] = useActionState(updateLocationTypeAction, initialState);

  useActionSuccessToast(state, "Location type updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!locationType) return null;

  const otherTypes = existingTypes.filter((t) => t.id !== locationType.id);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Edit location type</DialogTitle>
        </DialogHeader>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={locationType.id} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-lt-name">Name</Label>
            <Input id="edit-lt-name" name="name" defaultValue={locationType.name} required />
          </div>
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="Allowed parent type"
              name="allowedParentTypeId"
              defaultValue={locationType.allowedParentTypeId ?? ""}
              options={[
                { value: "", label: "None (top-level)" },
                ...otherTypes.map((t) => ({ value: t.id, label: t.name })),
              ]}
            />
          </div>
          <div className="flex items-center gap-2">
            <Checkbox
              id="edit-lt-can-have-children"
              name="canHaveChildren"
              value="true"
              defaultChecked={locationType.canHaveChildren}
            />
            <Label htmlFor="edit-lt-can-have-children">Can have children</Label>
          </div>
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="Status"
              name="status"
              defaultValue={locationType.status}
              options={[
                { value: LocationTypeStatus.Active, label: "Active" },
                { value: LocationTypeStatus.Inactive, label: "Inactive" },
              ]}
            />
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
