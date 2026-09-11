"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getApprovalDocumentTypeById } from "@/modules/approvals/api/document-types.api";
import type { ApprovalDocumentTypeDto } from "@/modules/approvals/types/document-type";

export interface GetDocumentTypeDetailState {
  data: ApprovalDocumentTypeDto | null;
  error?: string;
}

export async function getDocumentTypeDetailAction(
  id: string,
): Promise<GetDocumentTypeDetailState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getApprovalDocumentTypeById(id);

  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load document type." };
  }

  return { data: result.data };
}
