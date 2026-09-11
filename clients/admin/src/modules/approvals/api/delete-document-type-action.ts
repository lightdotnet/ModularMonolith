"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { deleteApprovalDocumentType } from "@/modules/approvals/api/document-types.api";

export interface DeleteDocumentTypeActionState {
  error?: string;
  success?: boolean;
}

export async function deleteDocumentTypeAction(
  id: string,
): Promise<DeleteDocumentTypeActionState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const result = await deleteApprovalDocumentType(id);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to delete document type." };
  }

  revalidatePath("/approvals/document-types");
  return { success: true };
}
