"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { MAX_LINES, isNumericId, isValidQuantity } from "@/modules/purchasing/common";
import { DEFINITIVE_CODES } from "@/modules/purchasing/common/server/with-status-code";
import { receivePurchaseOrder } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";
import { DELIVERY_NOTE_MAX_LENGTH } from "@/modules/purchasing/purchase-orders/types/purchase-order";

export interface ReceivePurchaseOrderActionState {
  error?: string;
  success?: boolean;
  receiptId?: string;
  /**
   * True when the backend deterministically refused the request (400/403/404/409). False for an ambiguous
   * failure (network error, timeout, 5xx): the receipt may or may not be recorded, so the caller must retry
   * with the SAME delivery note and lines: the backend dedupes a receipt by delivery note reference.
   */
  definitive?: boolean;
}

export interface ReceivePurchaseOrderLineInput {
  purchaseOrderLineId: string;
  quantity: number;
}

/**
 * Records a goods receipt against an Approved/PartiallyReceived purchase order. The delivery note reference
 * is REQUIRED: it is the backend's only idempotency key, so a retry after a timeout cannot double-record.
 */
export async function receivePurchaseOrderAction(
  purchaseOrderId: string,
  deliveryNoteRef: string,
  receivedAt: string,
  lines: ReceivePurchaseOrderLineInput[],
): Promise<ReceivePurchaseOrderActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(purchaseOrderId)) {
    return { error: "Invalid purchase order." };
  }

  const note = deliveryNoteRef.trim();
  if (!note) {
    return { error: "A delivery note reference is required." };
  }

  if (note.length > DELIVERY_NOTE_MAX_LENGTH) {
    return { error: `Delivery note must not exceed ${DELIVERY_NOTE_MAX_LENGTH} characters.` };
  }

  const receivedDate = new Date(receivedAt);
  if (Number.isNaN(receivedDate.getTime())) {
    return { error: "Received date is invalid." };
  }

  // A minute of tolerance for clock skew between the browser and this server.
  if (receivedDate.getTime() > Date.now() + 60_000) {
    return { error: "Received date cannot be in the future." };
  }

  if (lines.length === 0) {
    return { error: "Enter a quantity for at least one line." };
  }

  if (lines.length > MAX_LINES) {
    return { error: `At most ${MAX_LINES} lines are allowed.` };
  }

  const seen = new Set<string>();
  for (const line of lines) {
    if (!isNumericId(line.purchaseOrderLineId)) {
      return { error: "Invalid purchase order line." };
    }
    if (seen.has(line.purchaseOrderLineId)) {
      return { error: "A line can appear only once per receipt." };
    }
    seen.add(line.purchaseOrderLineId);
    if (!isValidQuantity(line.quantity)) {
      return { error: "Quantities must be whole numbers between 1 and 1,000,000." };
    }
  }

  const result = await receivePurchaseOrder(purchaseOrderId, {
    deliveryNoteRef: note,
    receivedAt: receivedDate.toISOString(),
    lines,
  });

  if (!result.isSuccess) {
    return {
      error: result.message || "Failed to record receipt.",
      definitive: DEFINITIVE_CODES.has(result.code),
    };
  }

  revalidatePath("/purchasing/orders");
  revalidatePath(`/purchasing/orders/${purchaseOrderId}`);
  revalidatePath("/purchasing/receipts");
  return { success: true, receiptId: result.data ? String(result.data) : undefined };
}
