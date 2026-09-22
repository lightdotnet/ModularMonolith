"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateStockTransfer } from "@/modules/transfers/api/transfers.api";
import { isNumericId } from "@/modules/transfers/utils/params";

export interface UpdateTransferFormState {
  error?: string;
  success?: boolean;
}

/** Updates a Draft transfer's header (source, destination, note). */
export async function updateTransferAction(
  _prevState: UpdateTransferFormState,
  formData: FormData,
): Promise<UpdateTransferFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const sourceLocationId = String(formData.get("sourceLocationId") ?? "").trim();
  const destinationLocationId = String(formData.get("destinationLocationId") ?? "").trim();
  const note = String(formData.get("note") ?? "").trim();

  if (!isNumericId(id)) {
    return { error: "Invalid transfer." };
  }

  if (!sourceLocationId || !destinationLocationId) {
    return { error: "Source and destination locations are required." };
  }

  if (sourceLocationId === destinationLocationId) {
    return { error: "Source and destination must be different locations." };
  }

  if (note.length > 1000) {
    return { error: "Note must not exceed 1000 characters." };
  }

  const result = await updateStockTransfer(id, {
    sourceLocationId,
    destinationLocationId,
    note: note || undefined,
  });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update transfer." };
  }

  revalidatePath("/transfers");
  revalidatePath(`/transfers/${id}`);
  return { success: true };
}
