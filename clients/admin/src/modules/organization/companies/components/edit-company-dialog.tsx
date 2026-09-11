"use client";

import { useActionState, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Combobox } from "@/components/ui/combobox";
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
  updateCompanyAction,
  type UpdateCompanyFormState,
} from "@/modules/organization/companies/api/update-company-action";
import { OrganizationStatus, type CompanyDto } from "@/modules/organization/companies/types/company";

const updateInitialState: UpdateCompanyFormState = {};

const STATUS_SELECT_OPTIONS = Object.values(OrganizationStatus).map((value) => ({
  value,
  label: value,
}));

interface EditCompanyDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  company: CompanyDto | null;
  onUpdated: () => void;
}

export function EditCompanyDialog({
  open,
  onOpenChange,
  company,
  onUpdated,
}: EditCompanyDialogProps) {
  const [state, formAction, pending] = useActionState(updateCompanyAction, updateInitialState);
  const [status, setStatus] = useState<OrganizationStatus>(
    company?.status ?? OrganizationStatus.Active,
  );

  useActionSuccessToast(state, "Company updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!company) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-lg"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Edit company</DialogTitle>
        </DialogHeader>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={company.id} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" defaultValue={company.name} required />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="code">Code</Label>
              <Input id="code" name="code" defaultValue={company.code} required />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="taxCode">Tax code</Label>
              <Input id="taxCode" name="taxCode" defaultValue={company.taxCode ?? ""} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="website">Website</Label>
              <Input id="website" name="website" defaultValue={company.website ?? ""} />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" name="email" type="email" defaultValue={company.email ?? ""} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="phone">Phone</Label>
              <Input id="phone" name="phone" defaultValue={company.phone ?? ""} />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="address">Address</Label>
            <Input id="address" name="address" defaultValue={company.address ?? ""} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="description">Description</Label>
            <Input id="description" name="description" defaultValue={company.description ?? ""} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="status">Status</Label>
            <Combobox
              id="status"
              name="status"
              value={status}
              onValueChange={setStatus}
              options={STATUS_SELECT_OPTIONS}
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
