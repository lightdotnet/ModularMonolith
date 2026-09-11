/** Mirrors backend `ApprovalPermissions.DocumentTypes` — keep the string values in sync with the backend. */
export const APPROVAL_DOCUMENT_TYPES_PERMISSIONS = {
  View: "approval.document_types.view",
  Create: "approval.document_types.create",
  Update: "approval.document_types.update",
  Delete: "approval.document_types.delete",
} as const;
