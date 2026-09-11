"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MoreHorizontal } from "lucide-react";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { removeEmployeeOrgUnitAction } from "@/modules/organization/employees/api/remove-employee-org-unit-action";
import { AssignOrgUnitDialog } from "@/modules/organization/employees/components/assign-org-unit-dialog";
import { UpdateMembershipDialog } from "@/modules/organization/employees/components/update-membership-dialog";
import { AssignmentType, type EmployeeMembershipDto } from "@/modules/organization/employees/types/employee";
import type { OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

interface EmployeeOrgUnitsTabProps {
  employeeId: string;
  memberships: EmployeeMembershipDto[];
  orgUnits: OrgUnitTreeNodeDto[];
  levels: EmployeeLevelDto[];
  canUpdate?: boolean;
  onChanged: () => void;
}

export function EmployeeOrgUnitsTab({
  employeeId,
  memberships,
  orgUnits,
  levels,
  canUpdate,
  onChanged,
}: EmployeeOrgUnitsTabProps) {
  const [assignOpen, setAssignOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [selected, setSelected] = useState<EmployeeMembershipDto | null>(null);
  const [removing, run] = useGuardedAction();

  const assignedIds = new Set(memberships.map((membership) => membership.orgUnitId));
  const assignableOrgUnits = orgUnits.filter((unit) => !assignedIds.has(unit.id));

  function handleRemove(membership: EmployeeMembershipDto) {
    run(
      () => removeEmployeeOrgUnitAction(employeeId, membership.orgUnitId),
      `Removed from "${membership.orgUnitName}".`,
      onChanged,
    );
  }

  return (
    <div className="flex flex-col gap-3">
      {canUpdate && (
        <div>
          <Button size="sm" onClick={() => setAssignOpen(true)} disabled={assignableOrgUnits.length === 0}>
            <Plus />
            Assign to department/team
          </Button>
        </div>
      )}

      {memberships.length === 0 ? (
        <p className="text-sm text-muted-foreground">Not assigned to any department or team yet.</p>
      ) : (
        <div className="flex flex-col gap-2">
          {memberships.map((membership) => (
            <div
              key={membership.orgUnitId}
              className="flex items-center gap-2 rounded-md border border-border p-2.5"
            >
              <div className="flex flex-1 flex-col">
                <div className="flex items-center gap-2">
                  <span className="font-medium">{membership.orgUnitName}</span>
                  <Badge variant="outline">{membership.orgUnitType}</Badge>
                  {membership.isPrimary && <Badge>Primary</Badge>}
                  {membership.assignmentType === AssignmentType.Acting && (
                    <Badge variant="secondary">Acting</Badge>
                  )}
                  {membership.isManager && <Badge variant="outline">Manager</Badge>}
                </div>
                <span className="text-xs text-muted-foreground">
                  {membership.levelName ?? "No level"}
                </span>
              </div>

              {canUpdate && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button aria-label="Membership actions" size="icon-xs" variant="outline">
                      <MoreHorizontal />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem
                      onClick={() => {
                        setSelected(membership);
                        setEditOpen(true);
                      }}
                    >
                      Edit
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      variant="destructive"
                      disabled={removing}
                      onClick={() => handleRemove(membership)}
                    >
                      Remove
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          ))}
        </div>
      )}

      <AssignOrgUnitDialog
        open={assignOpen}
        onOpenChange={setAssignOpen}
        employeeId={employeeId}
        orgUnits={assignableOrgUnits}
        levels={levels}
        onAssigned={onChanged}
      />
      <UpdateMembershipDialog
        open={editOpen}
        onOpenChange={setEditOpen}
        employeeId={employeeId}
        membership={selected}
        levels={levels}
        onUpdated={onChanged}
      />
    </div>
  );
}
