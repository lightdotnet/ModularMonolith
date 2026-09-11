/** Mirrors `LeaveManagement.Contracts/LeaveRequests/LeaveType.cs` — serialized by name. */
export enum LeaveType {
  Annual = "Annual",
  Sick = "Sick",
  Unpaid = "Unpaid",
  Other = "Other",
}

/** Mirrors `LeaveManagement.Contracts/LeaveRequests/LeaveRequestStatus.cs` — serialized by name. */
export enum LeaveRequestStatus {
  Pending = "Pending",
  Approved = "Approved",
  Rejected = "Rejected",
  Cancelled = "Cancelled",
}

export interface LeaveRequestDto {
  id: string;
  userId: string;
  employeeId: string;
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  reason?: string | null;
  status: LeaveRequestStatus;
  approvalRequestId?: string | null;
  created: string;
}

export interface ApproverCandidateDto {
  employeeId: string;
  userId: string;
  name: string;
}

export interface CreateLeaveRequestPayload {
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  reason?: string;
  approverEmployeeId: string;
}

/** `approverEmployeeId` is only meaningful (and required) on the self-service resubmission path —
 * a manage-scoped edit is metadata-only and never touches Approval. */
export interface UpdateLeaveRequestPayload {
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  reason?: string;
  approverEmployeeId?: string;
}

export interface LeaveRequestSearchParams {
  leaveType?: LeaveType;
  status?: LeaveRequestStatus;
  /** Only honored server-side for callers with `leave.requests.manage`. */
  employeeId?: string;
  pageNumber?: number;
  pageSize?: number;
}
