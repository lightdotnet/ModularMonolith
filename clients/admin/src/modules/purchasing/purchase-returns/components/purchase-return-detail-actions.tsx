"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Ban, BadgeCheck, Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { RefreshButton } from "@/modules/purchasing/common";
import {
  CancelPurchaseReturnDialog,
  CreditPurchaseReturnDialog,
  PostPurchaseReturnDialog,
} from "@/modules/purchasing/purchase-returns/components/purchase-return-dialogs";

/** Gates are decided on the server page (permission + state) — this component only renders them. */
interface PurchaseReturnDetailActionsProps {
  returnId: string;
  returnNumber: string;
  locationName: string;
  purchaseOrderId: string;
  expectedCredit?: number | null;
  canPost: boolean;
  canCancel: boolean;
  canCredit: boolean;
  /** The stock movement is still being finalized: no mutating action, just a refresh. */
  processing: boolean;
}

export function PurchaseReturnDetailActions({
  returnId,
  returnNumber,
  locationName,
  purchaseOrderId,
  expectedCredit,
  canPost,
  canCancel,
  canCredit,
  processing,
}: PurchaseReturnDetailActionsProps) {
  const router = useRouter();
  const [postOpen, setPostOpen] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [creditOpen, setCreditOpen] = useState(false);

  const refresh = () => router.refresh();

  if (!canPost && !canCancel && !canCredit && !processing) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {processing && <RefreshButton />}
      {canPost && (
        <Button size="sm" onClick={() => setPostOpen(true)}>
          <Send className="size-4" />
          Post
        </Button>
      )}
      {canCredit && (
        <Button size="sm" onClick={() => setCreditOpen(true)}>
          <BadgeCheck className="size-4" />
          Mark credited
        </Button>
      )}
      {canCancel && (
        <Button size="sm" variant="destructive" onClick={() => setCancelOpen(true)}>
          <Ban className="size-4" />
          Cancel
        </Button>
      )}

      {canPost && (
        <PostPurchaseReturnDialog
          open={postOpen}
          onOpenChange={setPostOpen}
          returnId={returnId}
          returnNumber={returnNumber}
          locationName={locationName}
          purchaseOrderId={purchaseOrderId}
          onDone={refresh}
        />
      )}
      {canCancel && (
        <CancelPurchaseReturnDialog
          open={cancelOpen}
          onOpenChange={setCancelOpen}
          returnId={returnId}
          returnNumber={returnNumber}
          onDone={refresh}
        />
      )}
      {canCredit && (
        <CreditPurchaseReturnDialog
          open={creditOpen}
          onOpenChange={setCreditOpen}
          returnId={returnId}
          returnNumber={returnNumber}
          initialAmount={expectedCredit}
          onDone={refresh}
        />
      )}
    </div>
  );
}
