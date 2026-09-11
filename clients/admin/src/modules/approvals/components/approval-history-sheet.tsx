"use client";

import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { ApprovalTimeline } from "@/modules/approvals/components/approval-timeline";
import type { ApprovalRequestDto } from "@/modules/approvals/types/approval";

interface ApprovalHistorySheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  request: ApprovalRequestDto | null;
  /** userId -> display name, resolved once by the page. */
  userNamesById: Map<string, string>;
}

/** Read-only side panel showing a request's full approver chain — each level's
 * status, who decided it, when, and their comment/reason — so a viewer can see
 * exactly where a request currently sits without opening the decide dialog. */
export function ApprovalHistorySheet({ open, onOpenChange, request, userNamesById }: ApprovalHistorySheetProps) {
  const requesterName =
    request &&
    (request.requesterName ||
      userNamesById.get(request.requesterUserId) ||
      request.requesterUserId);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-md">
        <SheetHeader>
          <SheetTitle>{request?.title}</SheetTitle>
        </SheetHeader>

        <div className="flex flex-col gap-4 overflow-y-auto px-4 pb-4">
          {requesterName && <p className="text-sm text-muted-foreground">Requester: {requesterName}</p>}

          {request?.content && (
            <p className="whitespace-pre-wrap break-words rounded-md border bg-muted/50 p-3 text-sm">
              {request.content}
            </p>
          )}

          {request && (
            <ApprovalTimeline
              steps={request.steps}
              currentLevel={request.currentLevel}
              requestStatus={request.status}
              userNamesById={userNamesById}
              orientation="vertical"
            />
          )}
        </div>
      </SheetContent>
    </Sheet>
  );
}
