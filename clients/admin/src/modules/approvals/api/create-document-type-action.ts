"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createApprovalDocumentType } from "@/modules/approvals/api/document-types.api";
import type { CreateApprovalDocumentTypeRequest } from "@/modules/approvals/types/document-type";

export interface CreateDocumentTypeFormState {
  error?: string;
  success?: boolean;
}

export async function createDocumentTypeAction(
  _prevState: CreateDocumentTypeFormState,
  formData: FormData,
): Promise<CreateDocumentTypeFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();

  if (!name || !code) {
    return { error: "Name and code are required." };
  }

  const request: CreateApprovalDocumentTypeRequest = {
    name,
    code,
    description: String(formData.get("description") ?? "").trim() || null,
    isActive: formData.get("isActive") === "true",
  };

  const result = await createApprovalDocumentType(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create document type." };
  }

  revalidatePath("/approvals/document-types");
  return { success: true };
}
