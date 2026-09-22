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
  updateTypeAction,
  type UpdateTypeFormState,
} from "@/modules/orders/api/update-type-action";
import { OrderTypeCategory, OrderTypeStatus, type OrderTypeDto } from "@/modules/orders/types/order-type";

const initialState: UpdateTypeFormState = {};

interface EditTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  type: OrderTypeDto | null;
  onUpdated: () => void;
}

export function EditTypeDialog({ open, onOpenChange, type, onUpdated }: EditTypeDialogProps) {
  const [state, formAction, pending] = useActionState(updateTypeAction, initialState);

  useActionSuccessToast(state, "Type updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!type) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Edit {type.category === OrderTypeCategory.Fee ? "fee" : "payment"} type</DialogTitle>
        </DialogHeader>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={type.id} />
          <input type="hidden" name="category" value={type.category} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-ot-name">Name</Label>
            <Input id="edit-ot-name" name="name" defaultValue={type.name} required />
          </div>
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="Status"
              name="status"
              defaultValue={type.status}
              options={[
                { value: OrderTypeStatus.Active, label: "Active" },
                { value: OrderTypeStatus.Inactive, label: "Inactive" },
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
