import { ApprovalStatus } from "@/modules/approvals/types/approval";

/** Badge variant per request status — shared by the approval tables and the request detail page. */
export const APPROVAL_STATUS_VARIANT: Record<
  ApprovalStatus,
  "default" | "outline" | "destructive" | "secondary"
> = {
  [ApprovalStatus.Pending]: "secondary",
  [ApprovalStatus.Approved]: "default",
  [ApprovalStatus.Rejected]: "destructive",
  [ApprovalStatus.Cancelled]: "outline",
};
