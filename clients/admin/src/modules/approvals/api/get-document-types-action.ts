"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getApprovalDocumentTypes } from "@/modules/approvals/api/document-types.api";
import type { ApprovalDocumentTypeDto } from "@/modules/approvals/types/document-type";

export interface GetApprovalDocumentTypesState {
  data: ApprovalDocumentTypeDto[] | null;
  error?: string;
}

/** Client-callable list of approval document types (used by the create-request dialog). */
export async function getApprovalDocumentTypesAction(
  params: { activeOnly?: boolean } = {},
): Promise<GetApprovalDocumentTypesState> {
  const session = await resolveSession();
  if (!session) {
    return { data: null, error: "Your session has expired. Please sign in again." };
  }

  const result = await getApprovalDocumentTypes(params);
  if (!result.isSuccess || !result.data) {
    return { data: null, error: result.message || "Failed to load document types." };
  }

  return { data: result.data };
}
