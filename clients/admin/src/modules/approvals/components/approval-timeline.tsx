"use client";

import { Badge } from "@/components/ui/badge";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { cn } from "@/lib/shared/utils";
import {
  ApprovalStatus,
  ApprovalStepStatus,
  type ApprovalStepDto,
} from "@/modules/approvals/types/approval";

interface ApprovalTimelineProps {
  steps: ApprovalStepDto[];
  currentLevel: number;
  requestStatus: ApprovalStatus;
  /** userId -> display name, resolved once by the page. */
  userNamesById: Map<string, string>;
  orientation: "vertical" | "horizontal";
}

const STEP_STATUS_VARIANT: Record<
  ApprovalStepStatus,
  "default" | "outline" | "destructive" | "secondary"
> = {
  [ApprovalStepStatus.Pending]: "secondary",
  [ApprovalStepStatus.Approved]: "default",
  [ApprovalStepStatus.Rejected]: "destructive",
  [ApprovalStepStatus.Skipped]: "outline",
};

/** Timeline dot fill per step outcome — awaiting decision gets its own pulsing ring
 * rather than reusing the "pending" (not-yet-reached) styling, so the current
 * step in the chain is visually distinct at a glance. */
function dotClasses(status: ApprovalStepStatus, isAwaiting: boolean) {
  if (isAwaiting) return "border-amber-500 bg-background animate-pulse";
  switch (status) {
    case ApprovalStepStatus.Approved:
      return "border-green-600 bg-green-600 dark:border-green-500 dark:bg-green-500";
    case ApprovalStepStatus.Rejected:
      return "border-destructive bg-destructive";
    case ApprovalStepStatus.Skipped:
      return "border-border bg-muted-foreground/30";
    default:
      return "border-border bg-background";
  }
}

interface ResolvedStep {
  step: ApprovalStepDto;
  isAwaiting: boolean;
}

function StepDetails({ step, isAwaiting, userNamesById }: ResolvedStep & { userNamesById: Map<string, string> }) {
  return (
    <>
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-sm font-medium">Level {step.level}</span>
        <Badge variant={STEP_STATUS_VARIANT[step.status]}>
          {isAwaiting ? "Awaiting decision" : step.status}
        </Badge>
      </div>
      <span className="text-xs break-words text-muted-foreground">
        Approver:{" "}
        {step.approverName ||
          userNamesById.get(step.approverUserId) ||
          step.approverEmployeeId}
      </span>
      {step.decidedAt && (
        <span className="text-xs break-words text-muted-foreground">
          Decided: <LocalDateTime value={step.decidedAt} />
        </span>
      )}
      {step.comment && (
        <p className="mt-1 whitespace-pre-wrap break-words rounded-md border bg-muted/50 p-2 text-sm">
          {step.comment}
        </p>
      )}
    </>
  );
}

function VerticalTimeline({
  resolvedSteps,
  userNamesById,
}: {
  resolvedSteps: ResolvedStep[];
  userNamesById: Map<string, string>;
}) {
  return (
    <div className="flex flex-col">
      {resolvedSteps.map(({ step, isAwaiting }, index) => {
        const isLast = index === resolvedSteps.length - 1;
        return (
          <div key={step.id} className="flex gap-3">
            <div className="flex flex-col items-center">
              <span
                className={cn(
                  "mt-1.5 size-3 shrink-0 rounded-full border-2",
                  dotClasses(step.status, isAwaiting),
                  isAwaiting && "ring-2 ring-amber-500/40",
                )}
              />
              {!isLast && <span className="w-px flex-1 bg-border" />}
            </div>
            <div className="flex flex-col gap-1 pb-5">
              <StepDetails step={step} isAwaiting={isAwaiting} userNamesById={userNamesById} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function HorizontalTimeline({
  resolvedSteps,
  userNamesById,
}: {
  resolvedSteps: ResolvedStep[];
  userNamesById: Map<string, string>;
}) {
  return (
    <div className="flex items-start gap-2">
      {resolvedSteps.map(({ step, isAwaiting }, index) => {
        const isLast = index === resolvedSteps.length - 1;
        return (
          <div key={step.id} className="flex min-w-40 flex-1 flex-col gap-3">
            <div className="flex items-center">
              <span
                className={cn(
                  "size-3 shrink-0 rounded-full border-2",
                  dotClasses(step.status, isAwaiting),
                  isAwaiting && "ring-2 ring-amber-500/40",
                )}
              />
              {!isLast && <span className="h-px flex-1 bg-border" />}
            </div>
            <div className="flex min-w-0 flex-col gap-1 pr-4">
              <StepDetails step={step} isAwaiting={isAwaiting} userNamesById={userNamesById} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

/** Renders a request's approver chain as a timeline — each level's status, who decided it,
 * when, and their comment. `vertical` is the compact side-panel layout; `horizontal` lays
 * the steps left-to-right (falling back to `vertical` on small screens). */
export function ApprovalTimeline({
  steps,
  currentLevel,
  requestStatus,
  userNamesById,
  orientation,
}: ApprovalTimelineProps) {
  const resolvedSteps: ResolvedStep[] = [...steps]
    .sort((a, b) => a.level - b.level)
    .map((step) => ({
      step,
      isAwaiting:
        requestStatus === ApprovalStatus.Pending &&
        step.level === currentLevel &&
        step.status === ApprovalStepStatus.Pending,
    }));

  if (resolvedSteps.length === 0) {
    return <p className="text-sm text-muted-foreground">No approval steps.</p>;
  }

  if (orientation === "vertical") {
    return <VerticalTimeline resolvedSteps={resolvedSteps} userNamesById={userNamesById} />;
  }

  return (
    <>
      <div className="sm:hidden">
        <VerticalTimeline resolvedSteps={resolvedSteps} userNamesById={userNamesById} />
      </div>
      <div className="hidden overflow-x-auto p-1 -m-1 sm:block">
        <HorizontalTimeline resolvedSteps={resolvedSteps} userNamesById={userNamesById} />
      </div>
    </>
  );
}
