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
  createOrgUnitAction,
  type CreateOrgUnitFormState,
} from "@/modules/organization/departments/api/create-org-unit-action";
import { OrgUnitType, type OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";

const initialState: CreateOrgUnitFormState = {};

interface FormValues {
  name: string;
  code: string;
  description: string;
}

const initialValues: FormValues = { name: "", code: "", description: "" };

interface CreateOrgUnitDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  companyId: string;
  parent: OrgUnitTreeNodeDto | null;
  type: OrgUnitType;
  onCreated: () => void;
}

export function CreateOrgUnitDialog({
  open,
  onOpenChange,
  companyId,
  parent,
  type,
  onCreated,
}: CreateOrgUnitDialogProps) {
  const [state, formAction, pending] = useActionState(createOrgUnitAction, initialState);
  const [values, setValues] = useState<FormValues>(initialValues);

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) setValues(initialValues);
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, `${type} created.`, () => {
    onCreated();
    onOpenChange(false);
    setValues(initialValues);
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="companyId" value={companyId} />
          <input type="hidden" name="parentId" value={parent?.id ?? ""} />
          <input type="hidden" name="type" value={type} />

          <DialogHeader>
            <DialogTitle>
              Add {type.toLowerCase()}
              {parent ? ` under "${parent.name}"` : ""}
            </DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="name">Name</Label>
            <Input id="name" name="name" value={values.name} onChange={setField("name")} required />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="code">Code</Label>
            <Input id="code" name="code" value={values.code} onChange={setField("code")} required />
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
