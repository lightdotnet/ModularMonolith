"use client";

import { useActionState, useEffect, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect } from "@/components/ui/native-select";
import { Spinner } from "@/components/ui/spinner";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { getEmployeeDetailAction } from "@/modules/organization/employees/api/get-employee-detail-action";
import {
  updateEmployeeAction,
  type UpdateEmployeeFormState,
} from "@/modules/organization/employees/api/update-employee-action";
import { EmployeeOrgUnitsTab } from "@/modules/organization/employees/components/employee-org-units-tab";
import { EmployeeLoginTab } from "@/modules/organization/employees/components/employee-login-tab";
import { EmploymentStatus, type EmployeeDto } from "@/modules/organization/employees/types/employee";
import { flattenOrgUnitTree, type OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";
import { getOrgUnitTreeAction } from "@/modules/organization/departments/api/get-org-unit-tree-action";
import { getEmployeeLevelsAction } from "@/modules/organization/departments/api/get-employee-levels-action";
import type { EmployeeLevelDto } from "@/modules/organization/departments/types/employee-level";

const updateInitialState: UpdateEmployeeFormState = {};

const STATUS_OPTIONS = Object.values(EmploymentStatus).map((value) => ({ value, label: value }));

interface EditEmployeeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  employee: EmployeeDto | null;
  canManageLogin?: boolean;
  onUpdated: () => void;
}

export function EditEmployeeDialog({
  open,
  onOpenChange,
  employee,
  canManageLogin,
  onUpdated,
}: EditEmployeeDialogProps) {
  const [state, formAction, pending] = useActionState(updateEmployeeAction, updateInitialState);

  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [detail, setDetail] = useState<EmployeeDto | null>(null);
  const [activeTab, setActiveTab] = useState("details");
  const [orgUnits, setOrgUnits] = useState<OrgUnitTreeNodeDto[]>([]);
  const [levels, setLevels] = useState<EmployeeLevelDto[]>([]);
  const [orgUnitsLoaded, setOrgUnitsLoaded] = useState(false);
  const [orgUnitsLoading, setOrgUnitsLoading] = useState(false);
  const [employmentStatus, setEmploymentStatus] = useState<EmploymentStatus>(EmploymentStatus.Active);

  // The employees list endpoint doesn't populate `memberships` — load the full
  // record once the dialog opens, same "fetch full detail on open" pattern as
  // edit-user-dialog.tsx / edit-role-dialog.tsx. The org-unit tree / level
  // picklists are only needed by the "Departments & Teams" tab, so they're
  // fetched lazily (below) the first time that tab is actually opened, rather
  // than always up front — most edits never touch that tab.
  useEffect(() => {
    if (!employee) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const detailResult = await getEmployeeDetailAction(employee.id);
      if (cancelled) return;

      if (!detailResult.data) {
        setLoadError(detailResult.error || "Unable to load employee details.");
        setLoading(false);
        return;
      }

      setDetail(detailResult.data);
      setEmploymentStatus(detailResult.data.employmentStatus);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch once per mount; the dialog remounts fresh (via `key`) on every open
  }, []);

  useEffect(() => {
    if (activeTab !== "org-units" || orgUnitsLoaded || !employee) return;

    let cancelled = false;

    (async () => {
      setOrgUnitsLoading(true);

      const [treeResult, levelsResult] = await Promise.all([
        getOrgUnitTreeAction(employee.companyId),
        getEmployeeLevelsAction(employee.companyId),
      ]);
      if (cancelled) return;

      setOrgUnits(treeResult.data ? flattenOrgUnitTree(treeResult.data) : []);
      setLevels(levelsResult.data ?? []);
      setOrgUnitsLoaded(true);
      setOrgUnitsLoading(false);
    })();

    return () => {
      cancelled = true;
    };
  }, [activeTab, orgUnitsLoaded, employee]);

  useActionSuccessToast(state, "Employee updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  function refetchDetail() {
    if (!detail) return;
    onUpdated();
    getEmployeeDetailAction(detail.id).then((result) => {
      if (result.data) setDetail(result.data);
    });
  }

  if (!employee) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-xl"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Edit employee</DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
            <Spinner />
            Loading employee details...
          </div>
        ) : loadError ? (
          <Alert variant="destructive">
            <AlertDescription>{loadError}</AlertDescription>
          </Alert>
        ) : (
          detail && (
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList>
                <TabsTrigger value="details">Details</TabsTrigger>
                <TabsTrigger value="org-units">Departments &amp; Teams</TabsTrigger>
                {canManageLogin && <TabsTrigger value="login">Login</TabsTrigger>}
              </TabsList>

              <TabsContent value="details">
                <form action={formAction} className="flex flex-col gap-4">
                  <input type="hidden" name="id" value={detail.id} />
                  <input type="hidden" name="employmentStatus" value={employmentStatus} />

                  {state.error && (
                    <Alert variant="destructive">
                      <AlertDescription>{state.error}</AlertDescription>
                    </Alert>
                  )}

                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="employeeCode">Employee code</Label>
                      <Input id="employeeCode" name="employeeCode" defaultValue={detail.employeeCode} required />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="employmentStatusSelect">Employment status</Label>
                      <NativeSelect
                        id="employmentStatusSelect"
                        value={employmentStatus}
                        onChange={setEmploymentStatus}
                        options={STATUS_OPTIONS}
                      />
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="firstName">First name</Label>
                      <Input id="firstName" name="firstName" defaultValue={detail.firstName} required />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="lastName">Last name</Label>
                      <Input id="lastName" name="lastName" defaultValue={detail.lastName} required />
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="email">Email</Label>
                      <Input id="email" name="email" type="email" defaultValue={detail.email ?? ""} />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="phoneNumber">Phone number</Label>
                      <Input id="phoneNumber" name="phoneNumber" defaultValue={detail.phoneNumber ?? ""} />
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="gender">Gender</Label>
                      <Input id="gender" name="gender" defaultValue={detail.gender ?? ""} />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="nationalId">National ID</Label>
                      <Input id="nationalId" name="nationalId" defaultValue={detail.nationalId ?? ""} />
                    </div>
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="address">Address</Label>
                    <Input id="address" name="address" defaultValue={detail.address ?? ""} />
                  </div>
                  <div className="grid grid-cols-3 gap-3">
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="dateOfBirth">Date of birth</Label>
                      <Input
                        id="dateOfBirth"
                        name="dateOfBirth"
                        type="date"
                        defaultValue={detail.dateOfBirth?.slice(0, 10) ?? ""}
                      />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="hireDate">Hire date</Label>
                      <Input
                        id="hireDate"
                        name="hireDate"
                        type="date"
                        defaultValue={detail.hireDate?.slice(0, 10) ?? ""}
                      />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="terminationDate">Termination date</Label>
                      <Input
                        id="terminationDate"
                        name="terminationDate"
                        type="date"
                        defaultValue={detail.terminationDate?.slice(0, 10) ?? ""}
                      />
                    </div>
                  </div>

                  <DialogFooter>
                    <DialogClose asChild>
                      <Button type="button" variant="outline">
                        Cancel
                      </Button>
                    </DialogClose>
                    <Button type="submit" loading={pending}>
                      Save
                    </Button>
                  </DialogFooter>
                </form>
              </TabsContent>

              <TabsContent value="org-units">
                {orgUnitsLoading ? (
                  <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
                    <Spinner />
                    Loading departments &amp; teams...
                  </div>
                ) : (
                  <EmployeeOrgUnitsTab
                    employeeId={detail.id}
                    memberships={detail.memberships}
                    orgUnits={orgUnits}
                    levels={levels}
                    canUpdate
                    onChanged={refetchDetail}
                  />
                )}
              </TabsContent>

              {canManageLogin && (
                <TabsContent value="login">
                  <EmployeeLoginTab
                    employeeId={detail.id}
                    userId={detail.userId}
                    defaultEmail={detail.email}
                    defaultPhoneNumber={detail.phoneNumber}
                    onChanged={refetchDetail}
                  />
                </TabsContent>
              )}
            </Tabs>
          )
        )}
      </DialogContent>
    </Dialog>
  );
}
