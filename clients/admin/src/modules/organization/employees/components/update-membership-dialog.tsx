"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { NativeSelect } from "@/components/ui/native-select";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { updateEmployeeMembershipAction } from "@/modules/organization/employees/api/update-employee-membership-action";
import { AssignmentType, type EmployeeMembershipDto } from "@/modules/organization/employees/types/employee";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

const ASSIGNMENT_TYPE_OPTIONS = [
  { value: AssignmentType.Current, label: "Current" },
  { value: AssignmentType.Acting, label: "Acting" },
];

interface UpdateMembershipDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  employeeId: string;
  membership: EmployeeMembershipDto | null;
  levels: EmployeeLevelDto[];
  onUpdated: () => void;
}

export function UpdateMembershipDialog({
  open,
  onOpenChange,
  employeeId,
  membership,
  levels,
  onUpdated,
}: UpdateMembershipDialogProps) {
  const [levelId, setLevelId] = useState(membership?.levelId ?? "");
  const [isPrimary, setIsPrimary] = useState(membership?.isPrimary ?? false);
  const [assignmentType, setAssignmentType] = useState<AssignmentType>(
    membership?.assignmentType ?? AssignmentType.Current,
  );
  const [isManager, setIsManager] = useState(membership?.isManager ?? false);
  const [saving, run] = useGuardedAction();

  if (!membership) return null;

  function handleConfirm() {
    if (!membership) return;

    run(
      () =>
        updateEmployeeMembershipAction(employeeId, membership.orgUnitId, {
          levelId: levelId || undefined,
          isPrimary,
          assignmentType,
          isManager,
        }),
      "Updated.",
      () => {
        onOpenChange(false);
        onUpdated();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Update &quot;{membership.orgUnitName}&quot; membership</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="levelId">Level</Label>
            <NativeSelect
              id="levelId"
              placeholder="No level"
              value={levelId}
              onChange={setLevelId}
              options={levels.map((level) => ({ value: level.id, label: level.name }))}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="assignmentType">Status</Label>
            <NativeSelect
              id="assignmentType"
              value={assignmentType}
              onChange={(value) => setAssignmentType(value as AssignmentType)}
              options={ASSIGNMENT_TYPE_OPTIONS}
            />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <Checkbox checked={isPrimary} onCheckedChange={(checked) => setIsPrimary(checked === true)} />
            Primary department/team
          </label>
          <label className="flex items-center gap-2 text-sm">
            <Checkbox checked={isManager} onCheckedChange={(checked) => setIsManager(checked === true)} />
            Manager of this department/team
          </label>
        </div>

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancel
            </Button>
          </DialogClose>
          <Button type="button" loading={saving} onClick={handleConfirm}>
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
