import { leaveManagementApi } from "@/lib/server/backend-api";

const { requestJson } = leaveManagementApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  ApproverCandidateDto,
  CreateLeaveRequestPayload,
  LeaveRequestDto,
  LeaveRequestSearchParams,
  UpdateLeaveRequestPayload,
} from "@/modules/leave-requests/types/leave-request";

export function searchLeaveRequests(params: LeaveRequestSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<LeaveRequestDto>>("leave_request/search", {
      method: "GET",
      query: {
        leaveType: params.leaveType,
        status: params.status,
        employeeId: params.employeeId,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getLeaveRequestById(id: string) {
  return guardCall(() => requestJson<Result<LeaveRequestDto>>(`leave_request/${id}`));
}

export function getApproverCandidates() {
  return guardCall(() => requestJson<Result<ApproverCandidateDto[]>>("leave_request/approvers"));
}

export function createLeaveRequest(request: CreateLeaveRequestPayload) {
  return guardCall(() =>
    requestJson<Result<string>>("leave_request", { method: "POST", body: request }),
  );
}

export function updateLeaveRequest(id: string, request: UpdateLeaveRequestPayload) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`leave_request/${id}`, { method: "PUT", body: request }),
  );
}

export function deleteLeaveRequest(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`leave_request/${id}`, { method: "DELETE" }),
  );
}
