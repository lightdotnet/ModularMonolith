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
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  updateTransferAction,
  type UpdateTransferFormState,
} from "@/modules/transfers/api/update-transfer-action";
import { LocationSelect } from "@/modules/transfers/components/location-select";
import type { StockTransferDto } from "@/modules/transfers/types/transfer";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

const initialState: UpdateTransferFormState = {};

interface EditTransferDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  transfer: StockTransferDto;
  locations: LocationTreeNodeDto[];
}

export function EditTransferDialog({ open, onOpenChange, transfer, locations }: EditTransferDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] overflow-y-auto"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Edit transfer</DialogTitle>
        </DialogHeader>
        <EditTransferForm
          transfer={transfer}
          locations={locations}
          onDone={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}

function EditTransferForm({
  transfer,
  locations,
  onDone,
}: {
  transfer: StockTransferDto;
  locations: LocationTreeNodeDto[];
  onDone: () => void;
}) {
  const router = useRouter();
  const [state, formAction, pending] = useActionState(updateTransferAction, initialState);
  const [sourceLocationId, setSourceLocationId] = useState(String(transfer.sourceLocationId));
  const [destinationLocationId, setDestinationLocationId] = useState(
    String(transfer.destinationLocationId),
  );
  const [note, setNote] = useState(transfer.note ?? "");

  useActionSuccessToast(state, "Transfer updated.", () => {
    router.refresh();
    onDone();
  });

  const sameLocation = !!sourceLocationId && sourceLocationId === destinationLocationId;

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="id" value={transfer.id} />

      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="edit-transfer-source">Source location</Label>
        <LocationSelect
          id="edit-transfer-source"
          name="sourceLocationId"
          value={sourceLocationId}
          onValueChange={setSourceLocationId}
          locations={locations}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="edit-transfer-destination">Destination location</Label>
        <LocationSelect
          id="edit-transfer-destination"
          name="destinationLocationId"
          value={destinationLocationId}
          onValueChange={setDestinationLocationId}
          locations={locations}
        />
        {sameLocation && (
          <p className="text-xs text-destructive">Source and destination must be different.</p>
        )}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="edit-transfer-note">Note</Label>
        <Textarea
          id="edit-transfer-note"
          name="note"
          maxLength={1000}
          value={note}
          onChange={(event) => setNote(event.target.value)}
        />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          Cancel
        </Button>
        <Button
          type="submit"
          loading={pending}
          disabled={!sourceLocationId || !destinationLocationId || sameLocation}
        >
          Save
        </Button>
      </DialogFooter>
    </form>
  );
}
