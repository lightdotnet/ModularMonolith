"use client";

import { useActionState, useState } from "react";
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
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  createDocumentTypeAction,
  type CreateDocumentTypeFormState,
} from "@/modules/approvals/api/create-document-type-action";

const initialState: CreateDocumentTypeFormState = {};

interface FormValues {
  name: string;
  code: string;
  description: string;
}

const initialValues: FormValues = { name: "", code: "", description: "" };

interface CreateDocumentTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: () => void;
}

export function CreateDocumentTypeDialog({
  open,
  onOpenChange,
  onCreated,
}: CreateDocumentTypeDialogProps) {
  const [state, formAction, pending] = useActionState(createDocumentTypeAction, initialState);
  const [values, setValues] = useState<FormValues>(initialValues);
  const [isActive, setIsActive] = useState(true);

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function reset() {
    setValues(initialValues);
    setIsActive(true);
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) reset();
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, "Document type created.", () => {
    onCreated();
    onOpenChange(false);
    reset();
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="isActive" value={isActive ? "true" : "false"} />

          <DialogHeader>
            <DialogTitle>Add document type</DialogTitle>
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
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              name="description"
              value={values.description}
              onChange={setField("description")}
            />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <Checkbox
              checked={isActive}
              onCheckedChange={(checked) => setIsActive(checked === true)}
            />
            Active
          </label>

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
