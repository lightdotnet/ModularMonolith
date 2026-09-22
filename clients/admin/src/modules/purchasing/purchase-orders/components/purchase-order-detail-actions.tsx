"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Ban, PackageCheck, Pencil, RefreshCw, Send, Undo2, XCircle } from "lucide-react";
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
import { PurchaseOrderStatus } from "@/modules/purchasing/common";
import { cancelPurchaseOrderAction } from "@/modules/purchasing/purchase-orders/api/cancel-purchase-order-action";
import { closePurchaseOrderAction } from "@/modules/purchasing/purchase-orders/api/close-purchase-order-action";
import { withdrawPurchaseOrderAction } from "@/modules/purchasing/purchase-orders/api/withdraw-purchase-order-action";
import { PurchaseOrderHeaderDialog } from "@/modules/purchasing/purchase-orders/components/purchase-order-header-dialog";
import { ReasonPurchaseOrderDialog } from "@/modules/purchasing/purchase-orders/components/reason-purchase-order-dialog";
import { ReceivePurchaseOrderDialog } from "@/modules/purchasing/purchase-orders/components/receive-purchase-order-dialog";
import { SubmitPurchaseOrderDialog } from "@/modules/purchasing/purchase-orders/components/submit-purchase-order-dialog";
import type { PurchaseOrderDto, SupplierOption } from "@/modules/purchasing/purchase-orders/types/purchase-order";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

/** Gates are decided on the server page (permission + state + requester) — this component only renders them. */
export interface PurchaseOrderActionFlags {
  edit: boolean;
  submit: boolean;
  withdraw: boolean;
  receive: boolean;
  close: boolean;
  cancel: boolean;
}

interface PurchaseOrderDetailActionsProps {
  order: PurchaseOrderDto;
  flags: PurchaseOrderActionFlags;
  /** A receipt is still posting: no mutating action, just a refresh. */
  processing: boolean;
  /** Empty unless `flags.edit` — only then are the header options needed. */
  locations: LocationTreeNodeDto[];
  suppliers: SupplierOption[];
}

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

export function PurchaseOrderDetailActions({
  order,
  flags,
  processing,
  locations,
  suppliers,
}: PurchaseOrderDetailActionsProps) {
  const router = useRouter();
  const [refreshing, startRefresh] = useTransition();
  const [editOpen, setEditOpen] = useState(false);
  const [submitOpen, setSubmitOpen] = useState(false);
  const [withdrawOpen, setWithdrawOpen] = useState(false);
  const [receiveOpen, setReceiveOpen] = useState(false);
  const [closeOpen, setCloseOpen] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);

  const refresh = () => router.refresh();
  const resubmit = order.status === PurchaseOrderStatus.Rejected;

  if (!Object.values(flags).some(Boolean) && !processing) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {processing && (
        <Button
          size="sm"
          variant="outline"
          loading={refreshing}
          onClick={() => startRefresh(() => router.refresh())}
        >
          <RefreshCw className="size-4" />
          Refresh
        </Button>
      )}
      {flags.edit && (
        <Button size="sm" variant="outline" onClick={() => setEditOpen(true)}>
          <Pencil className="size-4" />
          Edit
        </Button>
      )}
      {flags.submit && (
        <Button size="sm" onClick={() => setSubmitOpen(true)}>
          <Send className="size-4" />
          {resubmit ? "Resubmit" : "Submit"}
        </Button>
      )}
      {flags.withdraw && (
        <Button size="sm" variant="outline" onClick={() => setWithdrawOpen(true)}>
          <Undo2 className="size-4" />
          Withdraw
        </Button>
      )}
      {flags.receive && (
        <Button size="sm" onClick={() => setReceiveOpen(true)}>
          <PackageCheck className="size-4" />
          Receive
        </Button>
      )}
      {flags.close && (
        <Button size="sm" variant="outline" onClick={() => setCloseOpen(true)}>
          <XCircle className="size-4" />
          Close
        </Button>
      )}
      {flags.cancel && (
        <Button size="sm" variant="destructive" onClick={() => setCancelOpen(true)}>
          <Ban className="size-4" />
          Cancel
        </Button>
      )}

      {flags.edit && (
        <PurchaseOrderHeaderDialog
          open={editOpen}
          onOpenChange={setEditOpen}
          locations={locations}
          suppliers={suppliers}
          order={order}
        />
      )}
      {flags.submit && (
        <SubmitPurchaseOrderDialog
          open={submitOpen}
          onOpenChange={setSubmitOpen}
          purchaseOrderId={order.id}
          poNumber={order.poNumber}
          resubmit={resubmit}
          onSubmitted={refresh}
        />
      )}
      {flags.withdraw && (
        <WithdrawDialog
          open={withdrawOpen}
          onOpenChange={setWithdrawOpen}
          purchaseOrderId={order.id}
          poNumber={order.poNumber}
          onDone={refresh}
        />
      )}
      {flags.receive && (
        <ReceivePurchaseOrderDialog
          open={receiveOpen}
          onOpenChange={setReceiveOpen}
          purchaseOrderId={order.id}
          poNumber={order.poNumber}
          locationName={order.locationName}
          lines={order.lines}
          onReceived={refresh}
        />
      )}
      {flags.close && (
        <ReasonPurchaseOrderDialog
          open={closeOpen}
          onOpenChange={setCloseOpen}
          title={`Close ${order.poNumber}`}
          description={`The ${formatQuantity(order.totalOutstandingQuantity)} unit(s) still outstanding will no longer be expected and the order will be closed. This cannot be undone. A reason is required.`}
          reasonLabel="Close reason"
          confirmLabel="Close order"
          successMessage="Purchase order closed."
          onSubmit={(reason) => closePurchaseOrderAction(order.id, reason)}
          onDone={refresh}
        />
      )}
      {flags.cancel && (
        <ReasonPurchaseOrderDialog
          open={cancelOpen}
          onOpenChange={setCancelOpen}
          title={`Cancel ${order.poNumber}`}
          description="This order will be abandoned; nothing has been received. This cannot be undone. A reason is required."
          reasonLabel="Cancellation reason"
          confirmLabel="Cancel order"
          successMessage="Purchase order cancelled."
          onSubmit={(reason) => cancelPurchaseOrderAction(order.id, reason)}
          onDone={refresh}
        />
      )}
    </div>
  );
}

function WithdrawDialog({
  open,
  onOpenChange,
  purchaseOrderId,
  poNumber,
  onDone,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  purchaseOrderId: string;
  poNumber: string;
  onDone: () => void;
}) {
  const [submitting, run] = useGuardedAction();

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Withdraw {poNumber}</DialogTitle>
          <DialogDescription>
            The pending approval is withdrawn and the order becomes editable again. You can submit it
            again afterwards.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button disabled={submitting} onClick={() => onOpenChange(false)} type="button" variant="outline">
            Back
          </Button>
          <Button
            loading={submitting}
            type="button"
            onClick={() =>
              run(() => withdrawPurchaseOrderAction(purchaseOrderId), "Purchase order withdrawn.", () => {
                onOpenChange(false);
                onDone();
              })
            }
          >
            Withdraw
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
