"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { activateSupplier, deactivateSupplier } from "@/modules/purchasing/suppliers/api/suppliers.api";

export interface SetSupplierStatusActionState {
  error?: string;
  success?: boolean;
}

/** Activates or deactivates a supplier. */
export async function setSupplierStatusAction(
  supplierId: string,
  activate: boolean,
): Promise<SetSupplierStatusActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  if (!isNumericId(supplierId)) {
    return { error: "Invalid supplier." };
  }

  const result = activate ? await activateSupplier(supplierId) : await deactivateSupplier(supplierId);

  if (!result.isSuccess) {
    return { error: result.message || `Failed to ${activate ? "activate" : "deactivate"} supplier.` };
  }

  revalidatePath("/purchasing/suppliers");
  return { success: true };
}
