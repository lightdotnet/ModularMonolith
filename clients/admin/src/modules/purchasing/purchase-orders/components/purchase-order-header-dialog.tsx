"use client";

import { useActionState, useState } from "react";
import { useRouter } from "next/navigation";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Combobox } from "@/components/ui/combobox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { toDateInputValue } from "@/modules/purchasing/common";
import { createPurchaseOrderAction } from "@/modules/purchasing/purchase-orders/api/create-purchase-order-action";
import { updatePurchaseOrderAction } from "@/modules/purchasing/purchase-orders/api/update-purchase-order-action";
import { LocationSelect } from "@/modules/purchasing/purchase-orders/components/location-select";
import {
  PURCHASE_ORDER_NOTE_MAX_LENGTH,
  type PurchaseOrderDto,
  type SupplierOption,
} from "@/modules/purchasing/purchase-orders/types/purchase-order";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface PurchaseOrderHeaderDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  locations: LocationTreeNodeDto[];
  /** Suppliers selectable for new/changed headers (active ones). */
  suppliers: SupplierOption[];
  /** Omit to create a new purchase order (phase 1: header only, then the detail page builds the lines). */
  order?: PurchaseOrderDto;
}

export function PurchaseOrderHeaderDialog({
  open,
  onOpenChange,
  locations,
  suppliers,
  order,
}: PurchaseOrderHeaderDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] overflow-y-auto"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>{order ? `Edit ${order.poNumber}` : "New purchase order"}</DialogTitle>
          {!order && (
            <DialogDescription>
              Create a draft first, then add the products on the next screen.
            </DialogDescription>
          )}
        </DialogHeader>
        {/* Mounted only while open, so every open starts from fresh form and action state. */}
        <PurchaseOrderHeaderForm
          locations={locations}
          suppliers={suppliers}
          order={order}
          onCancel={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}

function PurchaseOrderHeaderForm({
  locations,
  suppliers,
  order,
  onCancel,
}: {
  locations: LocationTreeNodeDto[];
  suppliers: SupplierOption[];
  order?: PurchaseOrderDto;
  onCancel: () => void;
}) {
  const router = useRouter();
  const [createState, createAction, createPending] = useActionState(createPurchaseOrderAction, {});
  const [updateState, updateAction, updatePending] = useActionState(updatePurchaseOrderAction, {});
  const state = order ? updateState : createState;
  const pending = order ? updatePending : createPending;

  const [supplierId, setSupplierId] = useState(order ? String(order.supplierId) : "");
  const [locationId, setLocationId] = useState(order ? String(order.locationId) : "");
  const [expectedAt, setExpectedAt] = useState(toDateInputValue(order?.expectedAt));
  const [note, setNote] = useState(order?.note ?? "");

  useActionSuccessToast(
    state,
    order ? "Purchase order updated." : "Draft purchase order created.",
    () => {
      if (order) {
        router.refresh();
        onCancel();
      } else if (createState.purchaseOrderId) {
        router.push(`/purchasing/orders/${createState.purchaseOrderId}`);
      }
    },
  );

  // Keep the current supplier selectable on edit even when it has since been deactivated.
  const options = suppliers.map((supplier) => ({
    value: supplier.id,
    label: `${supplier.name} (${supplier.code})`,
  }));
  if (order && !options.some((option) => option.value === String(order.supplierId))) {
    options.unshift({ value: String(order.supplierId), label: order.supplierName });
  }

  return (
    <form action={order ? updateAction : createAction} className="flex flex-col gap-4">
      {order && <input type="hidden" name="id" value={order.id} />}

      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="po-supplier">Supplier</Label>
        <Combobox
          id="po-supplier"
          name="supplierId"
          value={supplierId || null}
          onValueChange={setSupplierId}
          placeholder="Select a supplier"
          options={options}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="po-location">Receiving location</Label>
        <LocationSelect
          id="po-location"
          name="locationId"
          value={locationId}
          onValueChange={setLocationId}
          locations={locations}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="po-expected">Expected delivery date</Label>
        <Input
          id="po-expected"
          name="expectedAt"
          type="date"
          value={expectedAt}
          onChange={(event) => setExpectedAt(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="po-note">Note</Label>
        <Textarea
          id="po-note"
          name="note"
          maxLength={PURCHASE_ORDER_NOTE_MAX_LENGTH}
          value={note}
          onChange={(event) => setNote(event.target.value)}
        />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" loading={pending} disabled={!supplierId || !locationId}>
          {order ? "Save" : "Create draft"}
        </Button>
      </DialogFooter>
    </form>
  );
}
