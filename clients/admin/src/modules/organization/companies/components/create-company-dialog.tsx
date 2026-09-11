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
  createCompanyAction,
  type CreateCompanyFormState,
} from "@/modules/organization/companies/api/create-company-action";

const initialState: CreateCompanyFormState = {};

interface FormValues {
  name: string;
  code: string;
  taxCode: string;
  address: string;
  phone: string;
  email: string;
  website: string;
  description: string;
}

const initialValues: FormValues = {
  name: "",
  code: "",
  taxCode: "",
  address: "",
  phone: "",
  email: "",
  website: "",
  description: "",
};

interface CreateCompanyDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: () => void;
}

export function CreateCompanyDialog({ open, onOpenChange, onCreated }: CreateCompanyDialogProps) {
  const [state, formAction, pending] = useActionState(createCompanyAction, initialState);
  const [values, setValues] = useState<FormValues>(initialValues);

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) setValues(initialValues);
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, "Company created.", () => {
    onCreated();
    onOpenChange(false);
    setValues(initialValues);
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        className="sm:max-w-lg"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <form action={formAction} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>Create company</DialogTitle>
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
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="taxCode">Tax code</Label>
              <Input id="taxCode" name="taxCode" value={values.taxCode} onChange={setField("taxCode")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="website">Website</Label>
              <Input id="website" name="website" value={values.website} onChange={setField("website")} />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" name="email" type="email" value={values.email} onChange={setField("email")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="phone">Phone</Label>
              <Input id="phone" name="phone" value={values.phone} onChange={setField("phone")} />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="address">Address</Label>
            <Input id="address" name="address" value={values.address} onChange={setField("address")} />
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
