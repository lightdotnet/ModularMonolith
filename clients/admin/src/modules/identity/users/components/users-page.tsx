import { Card, CardContent } from "@/components/ui/card";
import { searchUsers } from "@/modules/identity/users/api/users.api";
import { UsersDataTable } from "@/modules/identity/users/components/users-data-table";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { USERS_PERMISSIONS } from "@/modules/identity/users/constants/permissions";

const PAGE_SIZE = 10;

interface UsersPageProps {
  searchParams: Promise<{ q?: string; page?: string }>;
}

export async function UsersPage({ searchParams }: UsersPageProps) {
  const { session, denied } = await requirePermission(USERS_PERMISSIONS.View);
  if (denied) return denied;

  const canCreate = hasPermission(session, USERS_PERMISSIONS.Create);
  const canUpdate = hasPermission(session, USERS_PERMISSIONS.Update);
  const canDelete = hasPermission(session, USERS_PERMISSIONS.Delete);

  const { q, page } = await searchParams;
  const pageNumber = Math.max(Number(page) || 1, 1);

  const result = await searchUsers({
    searchValue: q,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load users", description: result.message || "Please try again." }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Users</h1>
        <p className="text-sm text-muted-foreground">
          Manage the accounts that can sign in to the workspace.
        </p>
      </div>

      <Card>
        <CardContent>
          <UsersDataTable
            records={paged?.records ?? []}
            searchValue={q ?? ""}
            pageNumber={paged?.pageNumber ?? pageNumber}
            pageSize={paged?.pageSize ?? PAGE_SIZE}
            totalPages={paged?.totalPages ?? 1}
            totalRecords={paged?.totalRecords ?? 0}
            error={error}
            canCreate={canCreate}
            canUpdate={canUpdate}
            canDelete={canDelete}
          />
        </CardContent>
      </Card>
    </div>
  );
}
