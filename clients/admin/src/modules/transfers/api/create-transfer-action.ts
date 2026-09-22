"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createStockTransfer } from "@/modules/transfers/api/transfers.api";

export interface CreateTransferFormState {
  error?: string;
  success?: boolean;
  transferId?: string;
}

/** Creates a Draft transfer; the client then navigates to its detail page to build the lines. */
export async function createTransferAction(
  _prevState: CreateTransferFormState,
  formData: FormData,
): Promise<CreateTransferFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const sourceLocationId = String(formData.get("sourceLocationId") ?? "").trim();
  const destinationLocationId = String(formData.get("destinationLocationId") ?? "").trim();
  const note = String(formData.get("note") ?? "").trim();

  if (!sourceLocationId || !destinationLocationId) {
    return { error: "Source and destination locations are required." };
  }

  if (sourceLocationId === destinationLocationId) {
    return { error: "Source and destination must be different locations." };
  }

  if (note.length > 1000) {
    return { error: "Note must not exceed 1000 characters." };
  }

  const result = await createStockTransfer({
    sourceLocationId,
    destinationLocationId,
    note: note || undefined,
  });

  if (!result.isSuccess || !result.data) {
    return { error: result.message || "Failed to create transfer." };
  }

  revalidatePath("/transfers");
  return { success: true, transferId: String(result.data) };
}
