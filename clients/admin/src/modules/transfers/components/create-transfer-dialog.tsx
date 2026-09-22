"use client";

import { useActionState, useState } from "react";
import { useRouter } from "next/navigation";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  createTransferAction,
  type CreateTransferFormState,
} from "@/modules/transfers/api/create-transfer-action";
import { LocationSelect } from "@/modules/transfers/components/location-select";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

const initialState: CreateTransferFormState = {};

interface CreateTransferDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  locations: LocationTreeNodeDto[];
}

/** Phase 1 of the two-phase flow: creates the draft header, then hands off to the detail page to build the lines. */
export function CreateTransferDialog({ open, onOpenChange, locations }: CreateTransferDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] overflow-y-auto"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>New transfer</DialogTitle>
          <DialogDescription>
            Create a draft first, then add the products on the next screen.
          </DialogDescription>
        </DialogHeader>
        <CreateTransferForm locations={locations} onCancel={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function CreateTransferForm({
  locations,
  onCancel,
}: {
  locations: LocationTreeNodeDto[];
  onCancel: () => void;
}) {
  const router = useRouter();
  const [state, formAction, pending] = useActionState(createTransferAction, initialState);
  const [sourceLocationId, setSourceLocationId] = useState("");
  const [destinationLocationId, setDestinationLocationId] = useState("");
  const [note, setNote] = useState("");

  useActionSuccessToast(state, "Draft transfer created.", () => {
    if (state.transferId) router.push(`/transfers/${state.transferId}`);
  });

  const sameLocation = !!sourceLocationId && sourceLocationId === destinationLocationId;

  return (
    <form action={formAction} className="flex flex-col gap-4">
      {state.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="transfer-source">Source location</Label>
        <LocationSelect
          id="transfer-source"
          name="sourceLocationId"
          value={sourceLocationId}
          onValueChange={setSourceLocationId}
          locations={locations}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="transfer-destination">Destination location</Label>
        <LocationSelect
          id="transfer-destination"
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
        <Label htmlFor="transfer-note">Note</Label>
        <Textarea
          id="transfer-note"
          name="note"
          maxLength={1000}
          value={note}
          onChange={(event) => setNote(event.target.value)}
        />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button
          type="submit"
          loading={pending}
          disabled={!sourceLocationId || !destinationLocationId || sameLocation}
        >
          Create draft
        </Button>
      </DialogFooter>
    </form>
  );
}
