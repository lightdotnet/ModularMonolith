"use client";

import { useActionState, useState } from "react";
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
  createRoleAction,
  type CreateRoleFormState,
} from "@/modules/identity/roles/api/create-role-action";

const initialState: CreateRoleFormState = {};

interface FormValues {
  name: string;
  description: string;
}

const initialValues: FormValues = {
  name: "",
  description: "",
};

interface CreateRoleDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: () => void;
}

export function CreateRoleDialog({
  open,
  onOpenChange,
  onCreated,
}: CreateRoleDialogProps) {
  const [state, formAction, pending] = useActionState(
    createRoleAction,
    initialState,
  );
  const [values, setValues] = useState<FormValues>(initialValues);

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) setValues(initialValues);
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, "Role created.", () => {
    onCreated();
    onOpenChange(false);
    setValues(initialValues);
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>Add role</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="name">Name</Label>
            <Input
              id="name"
              name="name"
              value={values.name}
              onChange={setField("name")}
              required
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="description">Description</Label>
            <Input
              id="description"
              name="description"
              value={values.description}
              onChange={setField("description")}
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
