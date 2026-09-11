import { LeaveRequestStatus } from "@/modules/leave-requests/types/leave-request";

/** Badge variant per request status — shared by the leave requests table and the detail page. */
export const LEAVE_REQUEST_STATUS_VARIANT: Record<
  LeaveRequestStatus,
  "default" | "outline" | "destructive" | "secondary"
> = {
  [LeaveRequestStatus.Pending]: "secondary",
  [LeaveRequestStatus.Approved]: "default",
  [LeaveRequestStatus.Rejected]: "destructive",
  [LeaveRequestStatus.Cancelled]: "outline",
};
