"use client";

import { useEffect, useState } from "react";
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
import { NativeSelect } from "@/components/ui/native-select";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { getPurchaseOrderApproversAction } from "@/modules/purchasing/purchase-orders/api/get-purchase-order-approvers-action";
import { submitPurchaseOrderAction } from "@/modules/purchasing/purchase-orders/api/submit-purchase-order-action";
import type { PurchaseOrderApproverDto } from "@/modules/purchasing/purchase-orders/types/purchase-order";

interface SubmitPurchaseOrderDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  purchaseOrderId: string;
  poNumber: string;
  /** True when resubmitting after a rejection (starts a new approval). */
  resubmit: boolean;
  onSubmitted: () => void;
}

export function SubmitPurchaseOrderDialog({
  open,
  onOpenChange,
  purchaseOrderId,
  poNumber,
  resubmit,
  onSubmitted,
}: SubmitPurchaseOrderDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>
            {resubmit ? "Resubmit" : "Submit"} {poNumber} for approval
          </DialogTitle>
          <DialogDescription>
            Choose who approves this order. The approver decides in the Approvals area; once approved you
            can record receipts.
            {resubmit ? " Resubmitting starts a new approval." : ""}
          </DialogDescription>
        </DialogHeader>
        {/* Mounted only while open, so the approver list is fetched fresh per open. */}
        <SubmitPurchaseOrderForm
          purchaseOrderId={purchaseOrderId}
          resubmit={resubmit}
          onCancel={() => onOpenChange(false)}
          onSubmitted={() => {
            onOpenChange(false);
            onSubmitted();
          }}
        />
      </DialogContent>
    </Dialog>
  );
}

function SubmitPurchaseOrderForm({
  purchaseOrderId,
  resubmit,
  onCancel,
  onSubmitted,
}: {
  purchaseOrderId: string;
  resubmit: boolean;
  onCancel: () => void;
  onSubmitted: () => void;
}) {
  const [submitting, run] = useGuardedAction();
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [approvers, setApprovers] = useState<PurchaseOrderApproverDto[]>([]);
  const [approverEmployeeId, setApproverEmployeeId] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    (async () => {
      const result = await getPurchaseOrderApproversAction();
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load approvers.");
      } else {
        setApprovers(result.data);
      }
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const options = approvers.map((approver) => ({
    value: approver.employeeId,
    label: approver.name || approver.employeeId,
  }));
  const noApprovers = !loading && !loadError && options.length === 0;

  function handleSubmit() {
    if (!approverEmployeeId) return;
    setError("");

    run(
      async () => {
        const result = await submitPurchaseOrderAction(purchaseOrderId, approverEmployeeId);
        if (result.error) setError(result.error);
        return result;
      },
      resubmit ? "Purchase order resubmitted." : "Purchase order submitted for approval.",
      onSubmitted,
    );
  }

  return (
    <>
      {(loadError || error) && (
        <Alert variant="destructive">
          <AlertDescription>{loadError || error}</AlertDescription>
        </Alert>
      )}
      {noApprovers && (
        <Alert variant="destructive">
          <AlertDescription>
            No approver is available for you — contact an administrator.
          </AlertDescription>
        </Alert>
      )}
      {!loadError && !noApprovers && (
        <NativeSelect
          id="po-approver"
          label="Approver"
          placeholder="Select an approver"
          options={options}
          value={approverEmployeeId}
          onChange={setApproverEmployeeId}
          loading={loading}
          required
        />
      )}

      <DialogFooter>
        <Button disabled={submitting} onClick={onCancel} type="button" variant="outline">
          Back
        </Button>
        <Button
          disabled={submitting || loading || !approverEmployeeId}
          loading={submitting}
          onClick={handleSubmit}
          type="button"
        >
          {resubmit ? "Resubmit" : "Submit"}
        </Button>
      </DialogFooter>
    </>
  );
}
