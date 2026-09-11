"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateCompany } from "@/modules/organization/companies/api/companies.api";
import { OrganizationStatus, type CompanyDto } from "@/modules/organization/companies/types/company";

export interface UpdateCompanyFormState {
  error?: string;
  success?: boolean;
}

export async function updateCompanyAction(
  _prevState: UpdateCompanyFormState,
  formData: FormData,
): Promise<UpdateCompanyFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();

  if (!id || !name || !code) {
    return { error: "Name and code are required." };
  }

  const company: CompanyDto = {
    id,
    name,
    code,
    taxCode: String(formData.get("taxCode") ?? "") || undefined,
    address: String(formData.get("address") ?? "") || undefined,
    phone: String(formData.get("phone") ?? "") || undefined,
    email: String(formData.get("email") ?? "") || undefined,
    website: String(formData.get("website") ?? "") || undefined,
    description: String(formData.get("description") ?? "") || undefined,
    status: (String(formData.get("status") ?? OrganizationStatus.Active) as OrganizationStatus),
  };

  const result = await updateCompany(company);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update company." };
  }

  revalidatePath("/organization/companies");
  return { success: true };
}
