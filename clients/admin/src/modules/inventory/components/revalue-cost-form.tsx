"use client";

import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { DialogFooter } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  revalueStockAction,
  type RevalueStockFormState,
} from "@/modules/inventory/api/revalue-stock-action";

const initialState: RevalueStockFormState = {};

interface RevalueCostFormProps {
  productId: string;
  locationId: string;
  onRevalued: () => void;
  onCancel: () => void;
}

/** Product and location are fixed by the stock-level row that opened the dialog, so they travel as hidden inputs. */
export function RevalueCostForm({ productId, locationId, onRevalued, onCancel }: RevalueCostFormProps) {
  const [state, formAction, pending] = useActionState(revalueStockAction, initialState);

  useActionSuccessToast(state, "Stock cost revalued.", onRevalued);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="productId" value={productId} />
      <input type="hidden" name="locationId" value={locationId} />

      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="revalue-unit-cost">New unit cost</Label>
        <Input
          id="revalue-unit-cost"
          name="unitCost"
          type="number"
          inputMode="decimal"
          step="0.0001"
          min="0.0001"
          required
        />
        <p className="text-xs text-muted-foreground">
          Applies to the whole quantity currently on hand. Must be greater than zero.
        </p>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="revalue-note">Note</Label>
        <Textarea id="revalue-note" name="note" maxLength={500} />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" loading={pending}>
          Revalue
        </Button>
      </DialogFooter>
    </form>
  );
}
