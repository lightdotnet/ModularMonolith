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
  createEmployeeLevelAction,
  type CreateEmployeeLevelFormState,
} from "@/modules/organization/departments/api/create-employee-level-action";

const initialState: CreateEmployeeLevelFormState = {};

interface FormValues {
  name: string;
  code: string;
  rank: string;
  description: string;
}

const initialValues: FormValues = { name: "", code: "", rank: "0", description: "" };

interface CreateEmployeeLevelDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  companyId: string;
  onCreated: () => void;
}

export function CreateEmployeeLevelDialog({
  open,
  onOpenChange,
  companyId,
  onCreated,
}: CreateEmployeeLevelDialogProps) {
  const [state, formAction, pending] = useActionState(createEmployeeLevelAction, initialState);
  const [values, setValues] = useState<FormValues>(initialValues);

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) setValues(initialValues);
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, "Level created.", () => {
    onCreated();
    onOpenChange(false);
    setValues(initialValues);
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="companyId" value={companyId} />

          <DialogHeader>
            <DialogTitle>Add employee level</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" value={values.name} onChange={setField("name")} required />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="code">Code</Label>
              <Input id="code" name="code" value={values.code} onChange={setField("code")} required />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="rank">Rank</Label>
            <Input
              id="rank"
              name="rank"
              type="number"
              value={values.rank}
              onChange={setField("rank")}
            />
            <p className="text-xs text-muted-foreground">Lower ranks list first (e.g. Junior = 0, Senior = 10).</p>
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
