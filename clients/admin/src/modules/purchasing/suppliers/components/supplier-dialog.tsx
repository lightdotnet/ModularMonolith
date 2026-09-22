"use client";

import { useActionState, useState } from "react";
import { useRouter } from "next/navigation";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { createSupplierAction } from "@/modules/purchasing/suppliers/api/create-supplier-action";
import { updateSupplierAction } from "@/modules/purchasing/suppliers/api/update-supplier-action";
import { SUPPLIER_LIMITS, type SupplierDto } from "@/modules/purchasing/suppliers/types/supplier";

interface SupplierDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Omit to create a new supplier. */
  supplier?: SupplierDto;
}

/** Create/edit dialog. The parent remounts it via `key` per open so form and action state reset. */
export function SupplierDialog({ open, onOpenChange, supplier }: SupplierDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] overflow-y-auto"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>{supplier ? `Edit ${supplier.code}` : "New supplier"}</DialogTitle>
        </DialogHeader>
        <SupplierForm supplier={supplier} onDone={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function SupplierForm({ supplier, onDone }: { supplier?: SupplierDto; onDone: () => void }) {
  const router = useRouter();
  const [state, formAction, pending] = useActionState(
    supplier ? updateSupplierAction : createSupplierAction,
    {},
  );
  const [code, setCode] = useState(supplier?.code ?? "");
  const [name, setName] = useState(supplier?.name ?? "");
  const [contactName, setContactName] = useState(supplier?.contactName ?? "");
  const [phone, setPhone] = useState(supplier?.phone ?? "");
  const [email, setEmail] = useState(supplier?.email ?? "");
  const [address, setAddress] = useState(supplier?.address ?? "");
  const [paymentTerms, setPaymentTerms] = useState(supplier?.paymentTerms ?? "");

  useActionSuccessToast(state, supplier ? "Supplier updated." : "Supplier created.", () => {
    router.refresh();
    onDone();
  });

  return (
    <form action={formAction} className="flex flex-col gap-4">
      {supplier && <input type="hidden" name="id" value={supplier.id} />}

      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="supplier-code">Code</Label>
          <Input
            id="supplier-code"
            name="code"
            maxLength={SUPPLIER_LIMITS.code}
            value={code}
            onChange={(event) => setCode(event.target.value)}
            required
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="supplier-name">Name</Label>
          <Input
            id="supplier-name"
            name="name"
            maxLength={SUPPLIER_LIMITS.name}
            value={name}
            onChange={(event) => setName(event.target.value)}
            required
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="supplier-contact">Contact name</Label>
          <Input
            id="supplier-contact"
            name="contactName"
            maxLength={SUPPLIER_LIMITS.contactName}
            value={contactName}
            onChange={(event) => setContactName(event.target.value)}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="supplier-phone">Phone</Label>
          <Input
            id="supplier-phone"
            name="phone"
            maxLength={SUPPLIER_LIMITS.phone}
            value={phone}
            onChange={(event) => setPhone(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="supplier-email">Email</Label>
        <Input
          id="supplier-email"
          name="email"
          type="email"
          maxLength={SUPPLIER_LIMITS.email}
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="supplier-address">Address</Label>
        <Input
          id="supplier-address"
          name="address"
          maxLength={SUPPLIER_LIMITS.address}
          value={address}
          onChange={(event) => setAddress(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="supplier-terms">Payment terms</Label>
        <Input
          id="supplier-terms"
          name="paymentTerms"
          maxLength={SUPPLIER_LIMITS.paymentTerms}
          value={paymentTerms}
          onChange={(event) => setPaymentTerms(event.target.value)}
        />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          Cancel
        </Button>
        <Button type="submit" loading={pending} disabled={!code.trim() || !name.trim()}>
          {supplier ? "Save" : "Create"}
        </Button>
      </DialogFooter>
    </form>
  );
}
