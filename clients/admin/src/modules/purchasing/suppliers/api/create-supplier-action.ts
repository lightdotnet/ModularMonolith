"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createSupplier } from "@/modules/purchasing/suppliers/api/suppliers.api";
import { readSupplierForm } from "@/modules/purchasing/suppliers/utils/supplier-form";

export interface CreateSupplierFormState {
  error?: string;
  success?: boolean;
}

export async function createSupplierAction(
  _prevState: CreateSupplierFormState,
  formData: FormData,
): Promise<CreateSupplierFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const parsed = readSupplierForm(formData);
  if ("error" in parsed) return { error: parsed.error };

  const result = await createSupplier(parsed.request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create supplier." };
  }

  revalidatePath("/purchasing/suppliers");
  return { success: true };
}
