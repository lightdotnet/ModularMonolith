"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { isNumericId } from "@/modules/purchasing/common";
import { updateSupplier } from "@/modules/purchasing/suppliers/api/suppliers.api";
import { readSupplierForm } from "@/modules/purchasing/suppliers/utils/supplier-form";

export interface UpdateSupplierFormState {
  error?: string;
  success?: boolean;
}

export async function updateSupplierAction(
  _prevState: UpdateSupplierFormState,
  formData: FormData,
): Promise<UpdateSupplierFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  if (!isNumericId(id)) {
    return { error: "Invalid supplier." };
  }

  const parsed = readSupplierForm(formData);
  if ("error" in parsed) return { error: parsed.error };

  const result = await updateSupplier(id, parsed.request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update supplier." };
  }

  revalidatePath("/purchasing/suppliers");
  return { success: true };
}
