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
import { assignEmployeeOrgUnitAction } from "@/modules/organization/employees/api/assign-employee-org-unit-action";
import { AssignmentType } from "@/modules/organization/employees/types/employee";
import type { OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

const ASSIGNMENT_TYPE_OPTIONS = [
  { value: AssignmentType.Current, label: "Current" },
  { value: AssignmentType.Acting, label: "Acting" },
];

interface AssignOrgUnitDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  employeeId: string;
  orgUnits: OrgUnitTreeNodeDto[];
  levels: EmployeeLevelDto[];
  onAssigned: () => void;
}

const NONE_VALUE = "";

export function AssignOrgUnitDialog({
  open,
  onOpenChange,
  employeeId,
  orgUnits,
  levels,
  onAssigned,
}: AssignOrgUnitDialogProps) {
  const [orgUnitId, setOrgUnitId] = useState("");
  const [levelId, setLevelId] = useState(NONE_VALUE);
  const [isPrimary, setIsPrimary] = useState(false);
  const [assignmentType, setAssignmentType] = useState<AssignmentType>(AssignmentType.Current);
  const [isManager, setIsManager] = useState(false);
  const [saving, run] = useGuardedAction();

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setOrgUnitId("");
      setLevelId(NONE_VALUE);
      setIsPrimary(false);
      setAssignmentType(AssignmentType.Current);
      setIsManager(false);
    }
    onOpenChange(nextOpen);
  }

  function handleConfirm() {
    if (!orgUnitId) return;

    run(
      () =>
        assignEmployeeOrgUnitAction(employeeId, {
          orgUnitId,
          levelId: levelId || undefined,
          isPrimary,
          assignmentType,
          isManager,
        }),
      "Assigned.",
      () => {
        handleOpenChange(false);
        onAssigned();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Assign to department/team</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="orgUnitId">Department/Team</Label>
            <NativeSelect
              id="orgUnitId"
              placeholder="Select a department or team"
              value={orgUnitId}
              onChange={setOrgUnitId}
              options={orgUnits.map((unit) => ({
                value: unit.id,
                label: `${unit.name} (${unit.type})`,
              }))}
            />
          </div>
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
          <Button type="button" loading={saving} disabled={!orgUnitId} onClick={handleConfirm}>
            Assign
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
