"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteCompany } from "@/modules/organization/companies/api/companies.api";

export interface DeleteCompanyActionState {
  error?: string;
  success?: boolean;
}

export async function deleteCompanyAction(id: string): Promise<DeleteCompanyActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteCompany(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete company." };
  }

  revalidatePath("/organization/companies");
  return { success: true };
}
