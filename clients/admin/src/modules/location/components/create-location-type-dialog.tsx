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
  createLocationTypeAction,
  type CreateLocationTypeFormState,
} from "@/modules/location/api/create-location-type-action";
import type { LocationTypeDto } from "@/modules/location/types/location-type";

const initialState: CreateLocationTypeFormState = {};

interface CreateLocationTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  existingTypes: LocationTypeDto[];
  onCreated: () => void;
}

export function CreateLocationTypeDialog({
  open,
  onOpenChange,
  existingTypes,
  onCreated,
}: CreateLocationTypeDialogProps) {
  const [state, formAction, pending] = useActionState(createLocationTypeAction, initialState);

  useActionSuccessToast(state, "Location type created.", () => {
    onCreated();
    onOpenChange(false);
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>Create location type</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lt-id">ID <span className="text-xs text-muted-foreground">(unique, e.g. &quot;country&quot;)</span></Label>
            <Input id="lt-id" name="id" required placeholder="e.g. country" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lt-name">Name</Label>
            <Input id="lt-name" name="name" required placeholder="e.g. Country" />
          </div>
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="Allowed parent type"
              name="allowedParentTypeId"
              options={[
                { value: "", label: "None (top-level)" },
                ...existingTypes.map((t) => ({ value: t.id, label: t.name })),
              ]}
            />
          </div>
          <div className="flex items-center gap-2">
            <Checkbox id="lt-can-have-children" name="canHaveChildren" value="true" />
            <Label htmlFor="lt-can-have-children">Can have children</Label>
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
