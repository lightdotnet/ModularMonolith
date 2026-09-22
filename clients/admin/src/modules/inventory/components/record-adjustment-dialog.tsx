"use client";

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { RecordAdjustmentForm } from "@/modules/inventory/components/record-adjustment-form";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface RecordAdjustmentDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  locations: LocationTreeNodeDto[];
  onRecorded: () => void;
}

export function RecordAdjustmentDialog({ open, onOpenChange, locations, onRecorded }: RecordAdjustmentDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Record stock adjustment</DialogTitle>
        </DialogHeader>
        <RecordAdjustmentForm
          locations={locations}
          onRecorded={() => {
            onRecorded();
            onOpenChange(false);
          }}
          onCancel={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}
