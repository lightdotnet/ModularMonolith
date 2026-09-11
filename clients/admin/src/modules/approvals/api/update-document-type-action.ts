"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { updateApprovalDocumentType } from "@/modules/approvals/api/document-types.api";
import type { ApprovalDocumentTypeDto } from "@/modules/approvals/types/document-type";

export interface UpdateDocumentTypeFormState {
  error?: string;
  success?: boolean;
}

export async function updateDocumentTypeAction(
  _prevState: UpdateDocumentTypeFormState,
  formData: FormData,
): Promise<UpdateDocumentTypeFormState> {
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

  const dto: ApprovalDocumentTypeDto = {
    id,
    name,
    code,
    description: String(formData.get("description") ?? "").trim() || null,
    isActive: formData.get("isActive") === "true",
    created: String(formData.get("created") ?? ""),
  };

  const result = await updateApprovalDocumentType(dto);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to update document type." };
  }

  revalidatePath("/approvals/document-types");
  return { success: true };
}
