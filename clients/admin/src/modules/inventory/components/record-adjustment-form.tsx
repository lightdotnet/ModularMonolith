"use client";

import { useActionState, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { DialogFooter } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  recordStockMovementAction,
  type RecordStockMovementFormState,
} from "@/modules/inventory/api/record-stock-movement-action";
import { LocationSelect } from "@/modules/inventory/components/location-select";
import { ProductSelect } from "@/modules/inventory/components/product-select";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";
import type { ProductDto } from "@/modules/catalog/types/product";

const initialState: RecordStockMovementFormState = {};

interface RecordAdjustmentFormProps {
  locations: LocationTreeNodeDto[];
  onRecorded: () => void;
  onCancel: () => void;
}

/**
 * Record Adjustment form body — split out from `RecordAdjustmentDialog` so the
 * dialog only owns open/remount state, same shape as `LoginForm` owning its own
 * `useActionState` inside a container that doesn't. `ProductSelect` isn't a plain
 * named form field (it hands back a `ProductDto`, not a string), so its id is
 * threaded into the form via a hidden input — same pattern as `imagesJson` in
 * `modules/catalog/components/product-panel.tsx`. `LocationSelect` wraps a
 * `Combobox`, which already renders its own hidden input for `name="locationId"`.
 */
export function RecordAdjustmentForm({ locations, onRecorded, onCancel }: RecordAdjustmentFormProps) {
  const [state, formAction, pending] = useActionState(recordStockMovementAction, initialState);
  const [product, setProduct] = useState<ProductDto | null>(null);
  const [locationId, setLocationId] = useState("");

  useActionSuccessToast(state, "Stock adjustment recorded.", onRecorded);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="productId" value={product?.id ?? ""} />

      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label>Product</Label>
        <ProductSelect value={product?.id ?? ""} onValueChange={setProduct} />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="adj-location">Location</Label>
        <LocationSelect
          id="adj-location"
          name="locationId"
          value={locationId}
          onValueChange={setLocationId}
          locations={locations}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="adj-quantity-delta">Quantity change</Label>
        <Input id="adj-quantity-delta" name="quantityDelta" type="number" step="1" required />
        <p className="text-xs text-muted-foreground">Positive adds stock, negative removes it.</p>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="adj-unit-cost">Unit cost</Label>
        <Input
          id="adj-unit-cost"
          name="unitCost"
          type="number"
          inputMode="decimal"
          step="0.0001"
          min="0"
        />
        <p className="text-xs text-muted-foreground">
          Applies to inbound movements. Leave empty to use the current average cost; required when no stock is on
          hand.
        </p>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="adj-note">Note</Label>
        <Textarea id="adj-note" name="note" maxLength={500} />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" loading={pending} disabled={!product || !locationId}>
          Record
        </Button>
      </DialogFooter>
    </form>
  );
}
