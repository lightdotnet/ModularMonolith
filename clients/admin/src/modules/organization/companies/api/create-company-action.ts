"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createCompany } from "@/modules/organization/companies/api/companies.api";
import type { CreateCompanyRequest } from "@/modules/organization/companies/types/company";

export interface CreateCompanyFormState {
  error?: string;
  success?: boolean;
}

export async function createCompanyAction(
  _prevState: CreateCompanyFormState,
  formData: FormData,
): Promise<CreateCompanyFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();

  if (!name || !code) {
    return { error: "Name and code are required." };
  }

  const request: CreateCompanyRequest = {
    name,
    code,
    taxCode: String(formData.get("taxCode") ?? "") || undefined,
    address: String(formData.get("address") ?? "") || undefined,
    phone: String(formData.get("phone") ?? "") || undefined,
    email: String(formData.get("email") ?? "") || undefined,
    website: String(formData.get("website") ?? "") || undefined,
    description: String(formData.get("description") ?? "") || undefined,
  };

  const result = await createCompany(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create company." };
  }

  revalidatePath("/organization/companies");
  return { success: true };
}
