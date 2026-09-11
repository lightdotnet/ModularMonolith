import { approvalApi } from "@/lib/server/backend-api";

const { requestJson } = approvalApi;
import { guardCall } from "@/lib/server/call-guard";
import type { PagedResult, Result } from "@/types/api";
import type {
  ApprovalRequestDto,
  ApprovalSearchParams,
  CreateApprovalRequestPayload,
} from "@/modules/approvals/types/approval";

/** Admin endpoints — every request, unrestricted. Require `approval.requests.view_all`. */
const APPROVAL_PATH = "approval";

export function searchApprovals(params: ApprovalSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<ApprovalRequestDto>>(APPROVAL_PATH, {
      method: "GET",
      query: {
        requestType: params.requestType,
        status: params.status,
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

/** Admin/test harness create — arbitrary requester + approver chain. */
export function createTestApprovalRequest(request: CreateApprovalRequestPayload) {
  return guardCall(() =>
    requestJson<Result<string>>(APPROVAL_PATH, { method: "POST", body: request }),
  );
}
