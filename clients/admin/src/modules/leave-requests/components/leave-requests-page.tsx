import { redirect } from "next/navigation";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { searchLeaveRequests } from "@/modules/leave-requests/api/leave-requests.api";
import { LeaveRequestsDataTable } from "@/modules/leave-requests/components/leave-requests-data-table";
import { LEAVE_REQUESTS_PERMISSIONS } from "@/modules/leave-requests/constants/permissions";
import { EMPLOYEE_ID_CLAIM_TYPE } from "@/modules/leave-requests/constants/claims";
import { hasPermission } from "@/lib/server/authorization";
import { resolveSession } from "@/modules/identity/user-profile";
import { searchEmployees } from "@/modules/organization/employees";

export async function LeaveRequestsPage() {
  const session = await resolveSession();
  if (!session || !session.profile) {
    redirect("/login");
  }

  const currentUserId = session.profile.id;
  const canManage = hasPermission(session, LEAVE_REQUESTS_PERMISSIONS.Manage);

  if (!canManage) {
    const result = await searchLeaveRequests({ pageSize: 100 });
    const error =
      !result.isSuccess || !result.data
        ? { title: "Unable to load leave requests", description: result.message || "Please try again." }
        : undefined;

    return (
      <div className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Leave requests</h1>
          <p className="text-sm text-muted-foreground">
            Submit and track your own leave requests.
          </p>
        </div>

        <LeaveRequestsDataTable
          records={result.data?.records ?? []}
          error={error}
          currentUserId={currentUserId}
          canManage={false}
          variant="mine"
        />
      </div>
    );
  }

  const currentEmployeeId = session.claims.find(
    (claim) => claim.type === EMPLOYEE_ID_CLAIM_TYPE,
  )?.value;

  const [myResult, allResult, employeesResult] = await Promise.all([
    searchLeaveRequests({ employeeId: currentEmployeeId, pageSize: 100 }),
    searchLeaveRequests({ pageSize: 100 }),
    searchEmployees({ pageSize: 200 }),
  ]);

  const myError =
    !myResult.isSuccess || !myResult.data
      ? { title: "Unable to load your requests", description: myResult.message || "Please try again." }
      : undefined;

  const allError =
    !allResult.isSuccess || !allResult.data
      ? { title: "Unable to load leave requests", description: allResult.message || "Please try again." }
      : undefined;

  // Best-effort — a manager might not also hold `organization.employees.view`, in which case this
  // stays empty and the table falls back to showing the raw employee id.
  const employeeNamesById = new Map(
    (employeesResult.data?.records ?? []).map((employee) => [
      employee.id,
      `${employee.firstName} ${employee.lastName}`.trim(),
    ]),
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Leave requests</h1>
        <p className="text-sm text-muted-foreground">
          Submit and track your own leave requests, or review everyone&apos;s.
        </p>
      </div>

      <Tabs defaultValue="mine">
        <TabsList>
          <TabsTrigger value="mine">My requests</TabsTrigger>
          <TabsTrigger value="all">All requests</TabsTrigger>
        </TabsList>

        <TabsContent value="mine">
          <LeaveRequestsDataTable
            records={myResult.data?.records ?? []}
            error={myError}
            currentUserId={currentUserId}
            canManage={canManage}
            variant="mine"
          />
        </TabsContent>

        <TabsContent value="all">
          <LeaveRequestsDataTable
            records={allResult.data?.records ?? []}
            error={allError}
            currentUserId={currentUserId}
            canManage={canManage}
            variant="all"
            employeeNamesById={employeeNamesById}
          />
        </TabsContent>
      </Tabs>
    </div>
  );
}
