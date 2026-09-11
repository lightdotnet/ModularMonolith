import Link from "next/link";
import { redirect } from "next/navigation";
import { ArrowLeft, ExternalLink } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { getLeaveRequestById } from "@/modules/leave-requests/api/leave-requests.api";
import { LeaveRequestDetailActions } from "@/modules/leave-requests/components/leave-request-detail-actions";
import { LEAVE_REQUESTS_PERMISSIONS } from "@/modules/leave-requests/constants/permissions";
import { LEAVE_REQUEST_STATUS_VARIANT } from "@/modules/leave-requests/constants/status-variant";
import { LeaveRequestStatus } from "@/modules/leave-requests/types/leave-request";
import { hasPermission } from "@/lib/server/authorization";
import { resolveSession } from "@/modules/identity/user-profile";

interface LeaveRequestDetailPageProps {
  id: string;
}

const EDITABLE_STATUSES = new Set([LeaveRequestStatus.Pending, LeaveRequestStatus.Rejected]);

export async function LeaveRequestDetailPage({ id }: LeaveRequestDetailPageProps) {
  const session = await resolveSession();
  if (!session?.profile) {
    redirect("/login");
  }
  const currentUserId = session.profile.id;
  const canManage = hasPermission(session, LEAVE_REQUESTS_PERMISSIONS.Manage);

  const detail = await getLeaveRequestById(id);

  if (!detail.isSuccess || !detail.data) {
    return (
      <div className="flex flex-col gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Request not found</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              This leave request does not exist, or you do not have access to it.
            </p>
            <Link
              href="/leave-requests"
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ArrowLeft className="size-4" />
              Back to leave requests
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  const request = detail.data;
  const isOwn = request.userId === currentUserId;
  const canEdit = isOwn && EDITABLE_STATUSES.has(request.status);
  const canDelete = canManage ? true : isOwn && EDITABLE_STATUSES.has(request.status);

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/leave-requests"
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to leave requests
      </Link>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-semibold tracking-tight">{request.leaveType} leave</h1>
              <Badge variant={LEAVE_REQUEST_STATUS_VARIANT[request.status]}>{request.status}</Badge>
            </div>
            <LeaveRequestDetailActions
              leaveRequest={request}
              canEdit={canEdit}
              canDelete={canDelete}
              canManage={canManage}
            />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Start date</dt>
              <dd className="text-sm">
                <LocalDateTime value={request.startDate} />
              </dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">End date</dt>
              <dd className="text-sm">
                <LocalDateTime value={request.endDate} />
              </dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Created</dt>
              <dd className="text-sm">
                <LocalDateTime value={request.created} />
              </dd>
            </div>
          </dl>

          {request.reason && (
            <p className="whitespace-pre-wrap break-words rounded-md border bg-muted/50 p-3 text-sm">
              {request.reason}
            </p>
          )}

          {request.approvalRequestId && (
            <Link
              href={`/approvals/requests/${request.approvalRequestId}`}
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              View approval progress
              <ExternalLink className="size-4" />
            </Link>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
