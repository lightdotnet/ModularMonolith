import { redirect } from "next/navigation";
import { ApprovalsTabs } from "@/modules/approvals/components/approvals-tabs";
import { hasPermission } from "@/lib/server/authorization";
import { resolveSession } from "@/modules/identity/user-profile";
import { getAllUsers } from "@/modules/identity/users/api/users.api";
import { getDisplayName } from "@/lib/shared/user-display";
import { APPROVALS_PERMISSIONS } from "@/modules/approvals/constants/permissions";

export async function ApprovalsPage() {
  const session = await resolveSession();
  if (!session || !session.profile) {
    redirect("/login");
  }

  const canViewAll = hasPermission(session, APPROVALS_PERMISSIONS.ViewAll);

  const usersResult = await getAllUsers();
  const userNamesById = new Map(
    (usersResult.data ?? []).map((user) => [user.id, getDisplayName(user)]),
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Approvals</h1>
        <p className="text-sm text-muted-foreground">
          Create requests, review what&apos;s waiting on you, and track requests you&apos;ve created
          or already decided.
        </p>
      </div>

      <ApprovalsTabs canViewAll={canViewAll} userNamesById={userNamesById} />
    </div>
  );
}
