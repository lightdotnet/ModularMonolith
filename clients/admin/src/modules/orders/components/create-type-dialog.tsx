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
  createTypeAction,
  type CreateTypeFormState,
} from "@/modules/orders/api/create-type-action";
import { OrderTypeCategory } from "@/modules/orders/types/order-type";

const initialState: CreateTypeFormState = {};

interface CreateTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: () => void;
}

export function CreateTypeDialog({
  open,
  onOpenChange,
  onCreated,
}: CreateTypeDialogProps) {
  const [state, formAction, pending] = useActionState(createTypeAction, initialState);

  useActionSuccessToast(state, "Type created.", () => {
    onCreated();
    onOpenChange(false);
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>Create type</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <NativeSelect<OrderTypeCategory>
            label="Category"
            name="category"
            defaultValue={OrderTypeCategory.Fee}
            options={[
              { value: OrderTypeCategory.Fee, label: "Fee Type" },
              { value: OrderTypeCategory.Payment, label: "Payment Type" },
            ]}
          />

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ot-id">ID <span className="text-xs text-muted-foreground">(unique, e.g. &quot;shipping&quot;)</span></Label>
            <Input id="ot-id" name="id" required placeholder="e.g. shipping" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ot-name">Name</Label>
            <Input id="ot-name" name="name" required placeholder="e.g. Shipping" />
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
