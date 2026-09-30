import { Card, CardContent } from "@/components/ui/card";
import { getAllRoles } from "@/modules/identity/roles/api/roles.api";
import { RolesDataTable } from "@/modules/identity/roles/components/roles-data-table";
import { ROLES_PERMISSIONS } from "@/modules/identity/roles/constants/permissions";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";

export async function RolesPage() {
  const { session, denied } = await requirePermission(ROLES_PERMISSIONS.View);
  if (denied) return denied;

  const canManage = hasPermission(session, ROLES_PERMISSIONS.Manage);

  const result = await getAllRoles();

  const error =
    !result.isSuccess || !result.data
      ? {
          title: "Unable to load roles",
          description: result.message || "Please try again.",
        }
      : undefined;
  const roles = result.data ?? [];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Roles</h1>
        <p className="text-sm text-muted-foreground">
          Manage roles and the permissions assigned to them.
        </p>
      </div>

      <Card>
        <CardContent>
          <RolesDataTable
            roles={roles}
            error={error}
            canManage={canManage}
          />
        </CardContent>
      </Card>
    </div>
  );
}
