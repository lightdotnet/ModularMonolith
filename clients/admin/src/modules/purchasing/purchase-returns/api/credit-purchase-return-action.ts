"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId, isValidAmountText } from "@/modules/purchasing/common";
import { markPurchaseReturnCredited } from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";
import { CREDIT_NOTE_NUMBER_MAX_LENGTH } from "@/modules/purchasing/purchase-returns/types/purchase-return";

export interface CreditPurchaseReturnActionState {
  error?: string;
  success?: boolean;
}

/** Records the supplier's credit note against a Posted return (informational bookkeeping). */
export async function creditPurchaseReturnAction(
  returnId: string,
  creditNoteNumber: string,
  creditAmount: string,
): Promise<CreditPurchaseReturnActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(returnId)) {
    return { error: "Invalid purchase return." };
  }

  const number = creditNoteNumber.trim();
  if (!number) {
    return { error: "Credit note number is required." };
  }

  if (number.length > CREDIT_NOTE_NUMBER_MAX_LENGTH) {
    return { error: `Credit note number must not exceed ${CREDIT_NOTE_NUMBER_MAX_LENGTH} characters.` };
  }

  if (!isValidAmountText(creditAmount)) {
    return { error: "Credit amount must be between 0 and 1,000,000,000 with at most 4 decimals." };
  }

  const result = await markPurchaseReturnCredited(returnId, {
    creditNoteNumber: number,
    creditAmount: Number(creditAmount.trim()),
  });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to mark purchase return credited." };
  }

  revalidatePath("/purchasing/returns");
  revalidatePath(`/purchasing/returns/${returnId}`);
  return { success: true };
}
