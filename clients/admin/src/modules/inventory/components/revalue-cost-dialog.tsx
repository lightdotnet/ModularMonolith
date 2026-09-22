"use client";

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { RevalueCostForm } from "@/modules/inventory/components/revalue-cost-form";

interface RevalueCostDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  productId: string;
  locationId: string;
  onRevalued: () => void;
}

export function RevalueCostDialog({
  open,
  onOpenChange,
  productId,
  locationId,
  onRevalued,
}: RevalueCostDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Revalue cost</DialogTitle>
        </DialogHeader>
        <RevalueCostForm
          productId={productId}
          locationId={locationId}
          onRevalued={() => {
            onRevalued();
            onOpenChange(false);
          }}
          onCancel={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}
