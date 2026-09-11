import { approvalApi } from "@/lib/server/backend-api";

const { requestJson } = approvalApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  ApprovalRequestDto,
  CreateApprovalRequestPayload,
  DecideApprovalPayload,
  MyApprovalsParams,
} from "@/modules/approvals/types/approval";

/** Self-service endpoints — requests the current user is related to (as requester or approver),
 * scoped server-side by `UserApprovalController`. */
const USER_APPROVAL_PATH = "approval/user";

/** `params.relation` narrows to one slice (requested by me / awaiting my decision / decided by
 * me); omitted, it returns all of them. */
export function getMyApprovals(params: MyApprovalsParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<ApprovalRequestDto>>(USER_APPROVAL_PATH, {
      method: "GET",
      query: {
        relation: params.relation,
        requestType: params.requestType,
        status: params.status,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function getApprovalById(id: string) {
  return guardCall(() => requestJson<Result<ApprovalRequestDto>>(`${USER_APPROVAL_PATH}/${id}`));
}

/** The backend overrides `requesterUserId` to the caller regardless of what's sent, so this
 * always creates as the current user. */
export function createApprovalRequest(request: CreateApprovalRequestPayload) {
  return guardCall(() =>
    requestJson<Result<string>>(USER_APPROVAL_PATH, { method: "POST", body: request }),
  );
}

export function decideApproval(id: string, request: DecideApprovalPayload) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`${USER_APPROVAL_PATH}/${id}/decide`, { method: "PUT", body: request }),
  );
}
