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
  createEmployeeAction,
  type CreateEmployeeFormState,
} from "@/modules/organization/employees/api/create-employee-action";
import { CompanySelect } from "@/modules/organization/companies/components/company-select";
import type { CompanyDto } from "@/modules/organization/companies/types/company";

const initialState: CreateEmployeeFormState = {};

interface FormValues {
  companyId: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  gender: string;
  nationalId: string;
  address: string;
  dateOfBirth: string;
  hireDate: string;
}

const initialValues: FormValues = {
  companyId: "",
  employeeCode: "",
  firstName: "",
  lastName: "",
  email: "",
  phoneNumber: "",
  gender: "",
  nationalId: "",
  address: "",
  dateOfBirth: "",
  hireDate: "",
};

interface CreateEmployeeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  companies: CompanyDto[];
  onCreated: () => void;
}

export function CreateEmployeeDialog({
  open,
  onOpenChange,
  companies,
  onCreated,
}: CreateEmployeeDialogProps) {
  const [state, formAction, pending] = useActionState(createEmployeeAction, initialState);
  const [values, setValues] = useState<FormValues>(initialValues);

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) setValues(initialValues);
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, "Employee created.", () => {
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
            <DialogTitle>Create employee</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="companyId">Company</Label>
            <CompanySelect
              id="companyId"
              name="companyId"
              companies={companies}
              value={values.companyId}
              onChange={(companyId) => setValues((previous) => ({ ...previous, companyId }))}
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="employeeCode">Employee code</Label>
              <Input
                id="employeeCode"
                name="employeeCode"
                value={values.employeeCode}
                onChange={setField("employeeCode")}
                required
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="dateOfBirth">Date of birth</Label>
              <Input
                id="dateOfBirth"
                name="dateOfBirth"
                type="date"
                value={values.dateOfBirth}
                onChange={setField("dateOfBirth")}
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="firstName">First name</Label>
              <Input
                id="firstName"
                name="firstName"
                value={values.firstName}
                onChange={setField("firstName")}
                required
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="lastName">Last name</Label>
              <Input
                id="lastName"
                name="lastName"
                value={values.lastName}
                onChange={setField("lastName")}
                required
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" name="email" type="email" value={values.email} onChange={setField("email")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="phoneNumber">Phone number</Label>
              <Input
                id="phoneNumber"
                name="phoneNumber"
                value={values.phoneNumber}
                onChange={setField("phoneNumber")}
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="gender">Gender</Label>
              <Input id="gender" name="gender" value={values.gender} onChange={setField("gender")} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nationalId">National ID</Label>
              <Input
                id="nationalId"
                name="nationalId"
                value={values.nationalId}
                onChange={setField("nationalId")}
              />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="address">Address</Label>
            <Input id="address" name="address" value={values.address} onChange={setField("address")} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="hireDate">Hire date</Label>
            <Input
              id="hireDate"
              name="hireDate"
              type="date"
              value={values.hireDate}
              onChange={setField("hireDate")}
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
