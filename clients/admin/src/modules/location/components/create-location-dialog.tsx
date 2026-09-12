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
import { NativeSelect } from "@/components/ui/native-select";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  createLocationAction,
  type CreateLocationFormState,
} from "@/modules/location/api/create-location-action";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";
import type { LocationTypeDto } from "@/modules/location/types/location-type";

const initialState: CreateLocationFormState = {};

interface CreateLocationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  parent: LocationTreeNodeDto | null;
  locationTypes: LocationTypeDto[];
  onCreated: () => void;
}

export function CreateLocationDialog({
  open,
  onOpenChange,
  parent,
  locationTypes,
  onCreated,
}: CreateLocationDialogProps) {
  const [state, formAction, pending] = useActionState(createLocationAction, initialState);

  useActionSuccessToast(state, "Location created.", () => {
    onCreated();
    onOpenChange(false);
  });

  const activeTypes = locationTypes.filter((t) => t.status === "Active");

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="parentLocationId" value={parent?.id ?? ""} />

          <DialogHeader>
            <DialogTitle>
              Add location{parent ? ` under "${parent.name}"` : ""}
            </DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="loc-name">Name</Label>
            <Input id="loc-name" name="name" required />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="loc-code">Code</Label>
            <Input id="loc-code" name="code" required />
          </div>
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="Location type"
              name="locationTypeId"
              required
              options={activeTypes.map((t) => ({ value: t.id, label: t.name }))}
            />
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
