"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Ban, PackageCheck, Pencil, RefreshCw, Send, XCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cancelTransferAction } from "@/modules/transfers/api/cancel-transfer-action";
import { closeTransferAction } from "@/modules/transfers/api/close-transfer-action";
import { DispatchTransferDialog } from "@/modules/transfers/components/dispatch-transfer-dialog";
import { EditTransferDialog } from "@/modules/transfers/components/edit-transfer-dialog";
import { ReasonTransferDialog } from "@/modules/transfers/components/reason-transfer-dialog";
import { ReceiveTransferDialog } from "@/modules/transfers/components/receive-transfer-dialog";
import { TransferReceiptStatus, TransferStatus, type StockTransferDto } from "@/modules/transfers/types/transfer";
import type { LocationTreeNodeDto } from "@/modules/location/types/location";

interface TransferDetailActionsProps {
  transfer: StockTransferDto;
  /** Empty unless the caller can edit the Draft header — only then are the location options needed. */
  locations: LocationTreeNodeDto[];
  /** True when the location list failed to load — the header editor is disabled rather than shown empty. */
  locationsUnavailable?: boolean;
  canCreate: boolean;
  canDispatch: boolean;
  canReceive: boolean;
  canClose: boolean;
}

function formatQuantity(quantity: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(quantity);
}

/**
 * State-appropriate, permission-gated actions. Draft: edit/dispatch/cancel. Dispatched or
 * PartiallyReceived with quantity still in transit: receive/close. Posting (or a receipt still
 * posting): no mutating action, just a refresh.
 */
export function TransferDetailActions({
  transfer,
  locations,
  locationsUnavailable,
  canCreate,
  canDispatch,
  canReceive,
  canClose,
}: TransferDetailActionsProps) {
  const router = useRouter();
  const [refreshing, startRefresh] = useTransition();
  const [editOpen, setEditOpen] = useState(false);
  const [dispatchOpen, setDispatchOpen] = useState(false);
  const [receiveOpen, setReceiveOpen] = useState(false);
  const [closeOpen, setCloseOpen] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);

  const refresh = () => router.refresh();

  const isDraft = transfer.status === TransferStatus.Draft;
  const isPosting = transfer.status === TransferStatus.Posting;
  const hasPostingReceipt = transfer.receipts.some(
    (receipt) => receipt.status === TransferReceiptStatus.Posting,
  );
  // While anything is still posting, Refresh is the only action (matches the Processing alert).
  const inTransit =
    !isPosting &&
    !hasPostingReceipt &&
    (transfer.status === TransferStatus.Dispatched || transfer.status === TransferStatus.PartiallyReceived) &&
    transfer.totalInTransitQuantity > 0;

  const showEdit = isDraft && canCreate;
  const showDispatch = isDraft && canDispatch;
  const showCancel = isDraft && canCreate;
  const showReceive = inTransit && canReceive;
  const showClose = inTransit && canClose;
  const showRefresh = isPosting || hasPostingReceipt;

  if (!showEdit && !showDispatch && !showCancel && !showReceive && !showClose && !showRefresh) {
    return null;
  }

  return (
    <div className="flex flex-wrap gap-2">
      {showRefresh && (
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
      {showEdit && (
        <Button
          size="sm"
          variant="outline"
          disabled={locationsUnavailable}
          title={locationsUnavailable ? "Locations could not be loaded" : undefined}
          onClick={() => setEditOpen(true)}
        >
          <Pencil className="size-4" />
          Edit
        </Button>
      )}
      {showDispatch && (
        <Button
          size="sm"
          disabled={transfer.lines.length === 0}
          title={transfer.lines.length === 0 ? "Add at least one line before dispatching" : undefined}
          onClick={() => setDispatchOpen(true)}
        >
          <Send className="size-4" />
          Dispatch
        </Button>
      )}
      {showReceive && (
        <Button size="sm" onClick={() => setReceiveOpen(true)}>
          <PackageCheck className="size-4" />
          Receive
        </Button>
      )}
      {showClose && (
        <Button size="sm" variant="outline" onClick={() => setCloseOpen(true)}>
          <XCircle className="size-4" />
          Close
        </Button>
      )}
      {showCancel && (
        <Button size="sm" variant="destructive" onClick={() => setCancelOpen(true)}>
          <Ban className="size-4" />
          Cancel
        </Button>
      )}

      {showEdit && (
        <EditTransferDialog
          open={editOpen}
          onOpenChange={setEditOpen}
          transfer={transfer}
          locations={locations}
        />
      )}
      {showDispatch && (
        <DispatchTransferDialog
          open={dispatchOpen}
          onOpenChange={setDispatchOpen}
          transferId={transfer.id}
          transferCode={transfer.transferCode}
          sourceLocationName={transfer.sourceLocationName}
          destinationLocationName={transfer.destinationLocationName}
          onDispatched={refresh}
        />
      )}
      {showReceive && (
        <ReceiveTransferDialog
          open={receiveOpen}
          onOpenChange={setReceiveOpen}
          transferId={transfer.id}
          transferCode={transfer.transferCode}
          destinationLocationName={transfer.destinationLocationName}
          lines={transfer.lines}
          onReceived={refresh}
        />
      )}
      {showClose && (
        <ReasonTransferDialog
          open={closeOpen}
          onOpenChange={setCloseOpen}
          title={`Close ${transfer.transferCode}`}
          description={`The ${formatQuantity(transfer.totalInTransitQuantity)} unit(s) still in transit will be written off as a variance and the transfer closed. This cannot be undone. A reason is required.`}
          reasonLabel="Close reason"
          confirmLabel="Close transfer"
          successMessage="Transfer closed."
          onSubmit={(reason) => closeTransferAction(transfer.id, reason)}
          onDone={refresh}
        />
      )}
      {showCancel && (
        <ReasonTransferDialog
          open={cancelOpen}
          onOpenChange={setCancelOpen}
          title={`Cancel ${transfer.transferCode}`}
          description="This draft will be abandoned; no stock has moved. This cannot be undone. A reason is required."
          reasonLabel="Cancellation reason"
          confirmLabel="Cancel transfer"
          successMessage="Transfer cancelled."
          onSubmit={(reason) => cancelTransferAction(transfer.id, reason)}
          onDone={refresh}
        />
      )}
    </div>
  );
}
