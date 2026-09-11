import Link from "next/link";
import { redirect } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { getApprovalById } from "@/modules/approvals/api/user-approvals.api";
import { ApprovalDecisionActions } from "@/modules/approvals/components/approval-decision-actions";
import { ApprovalTimeline } from "@/modules/approvals/components/approval-timeline";
import { APPROVAL_STATUS_VARIANT } from "@/modules/approvals/constants/status-variant";
import { getDisplayName } from "@/lib/shared/user-display";
import { resolveSession } from "@/modules/identity/user-profile";
import { getAllUsers } from "@/modules/identity/users/api/users.api";

interface ApprovalRequestDetailPageProps {
  id: string;
}

export async function ApprovalRequestDetailPage({ id }: ApprovalRequestDetailPageProps) {
  const session = await resolveSession();
  if (!session?.profile) {
    redirect("/login");
  }
  const currentUserId = session.profile.id;

  const [detail, usersResult] = await Promise.all([getApprovalById(id), getAllUsers()]);

  if (!detail.isSuccess || !detail.data) {
    return (
      <div className="flex flex-col gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Request not found</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              This approval request does not exist, or you do not have access to it.
            </p>
            <Link
              href="/approvals/requests"
              className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-primary hover:underline"
            >
              <ArrowLeft className="size-4" />
              Back to approvals
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  const request = detail.data;
  const userNamesById = new Map(
    (usersResult.data ?? []).map((user) => [user.id, getDisplayName(user)]),
  );
  const requesterName =
    request.requesterName ||
    userNamesById.get(request.requesterUserId) ||
    request.requesterUserId;

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/approvals/requests"
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to approvals
      </Link>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-2xl font-semibold tracking-tight">{request.title}</h1>
            <Badge variant={APPROVAL_STATUS_VARIANT[request.status]}>{request.status}</Badge>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Requester</dt>
              <dd className="text-sm">{requesterName}</dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Type</dt>
              <dd className="text-sm">{request.requestType}</dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Document type</dt>
              <dd className="text-sm">{request.documentTypeName}</dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Level</dt>
              <dd className="text-sm">
                {request.currentLevel} / {request.steps.length}
              </dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Created</dt>
              <dd className="text-sm">
                <LocalDateTime value={request.created} />
              </dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted-foreground">Finalized</dt>
              <dd className="text-sm">
                <LocalDateTime value={request.finalizedAt} />
              </dd>
            </div>
          </dl>

          {request.content && (
            <p className="whitespace-pre-wrap break-words rounded-md border bg-muted/50 p-3 text-sm">
              {request.content}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <CardTitle>Approval progress</CardTitle>
            <ApprovalDecisionActions request={request} currentUserId={currentUserId} />
          </div>
        </CardHeader>
        <CardContent>
          <ApprovalTimeline
            steps={request.steps}
            currentLevel={request.currentLevel}
            requestStatus={request.status}
            userNamesById={userNamesById}
            orientation="horizontal"
          />
        </CardContent>
      </Card>
    </div>
  );
}
