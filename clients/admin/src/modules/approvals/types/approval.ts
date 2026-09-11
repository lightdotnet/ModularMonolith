/** Mirrors `Approval.Contracts/Approvals/ApprovalStatus.cs` — serialized by name. */
export enum ApprovalStatus {
  Pending = "Pending",
  Approved = "Approved",
  Rejected = "Rejected",
  Cancelled = "Cancelled",
}

/** Mirrors `Approval.Contracts/Approvals/ApprovalStepStatus.cs` — serialized by name. */
export enum ApprovalStepStatus {
  Pending = "Pending",
  Approved = "Approved",
  Rejected = "Rejected",
  Skipped = "Skipped",
}

/** Mirrors `Approval.Contracts/Approvals/ApprovalRelation.cs` — serialized by name. */
export enum ApprovalRelation {
  All = "All",
  Requested = "Requested",
  AwaitingMyDecision = "AwaitingMyDecision",
  DecidedByMe = "DecidedByMe",
}

export interface ApprovalStepDto {
  id: string;
  approvalRequestId: string;
  level: number;
  approverUserId: string;
  approverEmployeeId: string;
  /** Display label for the approver, captured at creation time. */
  approverName?: string | null;
  status: ApprovalStepStatus;
  comment?: string | null;
  decidedAt?: string | null;
}

export interface ApprovalRequestDto {
  id: string;
  requestType: string;
  requestId: string;
  requesterUserId: string;
  requesterEmployeeId?: string | null;
  /** Display label for the requester, captured at creation time. */
  requesterName?: string | null;
  title: string;
  content?: string | null;
  deepLinkUrl?: string | null;
  currentLevel: number;
  status: ApprovalStatus;
  documentTypeId?: string | null;
  documentTypeName?: string | null;
  created: string;
  finalizedAt?: string | null;
  steps: ApprovalStepDto[];
}

export interface ApproverStepInput {
  level: number;
  approverUserId: string;
  approverEmployeeId: string;
  approverName?: string;
}

export interface CreateApprovalRequestPayload {
  requestType: string;
  requestId: string;
  requesterUserId: string;
  requesterEmployeeId?: string;
  requesterName?: string;
  title: string;
  content?: string;
  deepLinkUrl?: string;
  documentTypeId?: string;
  approverChain: ApproverStepInput[];
}

export interface DecideApprovalPayload {
  approved: boolean;
  comment?: string;
}

export interface ApprovalSearchParams {
  requestType?: string;
  status?: ApprovalStatus;
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface MyApprovalsParams {
  relation?: ApprovalRelation;
  requestType?: string;
  status?: ApprovalStatus;
  searchValue?: string;
  pageNumber?: number;
  pageSize?: number;
}
