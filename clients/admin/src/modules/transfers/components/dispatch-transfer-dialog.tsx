"use client";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { dispatchTransferAction } from "@/modules/transfers/api/dispatch-transfer-action";

interface DispatchTransferDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  transferId: string;
  transferCode: string;
  sourceLocationName: string;
  destinationLocationName: string;
  onDispatched: () => void;
}

export function DispatchTransferDialog({
  open,
  onOpenChange,
  transferId,
  transferCode,
  sourceLocationName,
  destinationLocationName,
  onDispatched,
}: DispatchTransferDialogProps) {
  const [dispatching, run] = useGuardedAction();

  function handleConfirm() {
    run(
      () => dispatchTransferAction(transferId),
      "Transfer dispatched.",
      () => {
        onOpenChange(false);
        onDispatched();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Dispatch {transferCode}</DialogTitle>
          <DialogDescription>
            The requested quantities leave the stock of {sourceLocationName} immediately and are in
            transit to {destinationLocationName} until received. A dispatched transfer can no longer
            be cancelled — it can only be received or closed. Dispatch fails if the source location
            does not have enough stock.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button disabled={dispatching} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button disabled={dispatching} loading={dispatching} onClick={handleConfirm} type="button">
            Dispatch
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
