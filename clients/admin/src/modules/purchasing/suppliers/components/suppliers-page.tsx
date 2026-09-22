import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import {
  PURCHASING_PERMISSIONS,
  SupplierStatus,
  parseEnumValue,
  parsePageNumber,
} from "@/modules/purchasing/common";
import { searchSuppliers } from "@/modules/purchasing/suppliers/api/suppliers.api";
import { SuppliersDataTable } from "@/modules/purchasing/suppliers/components/suppliers-data-table";

const PAGE_SIZE = 10;

interface SuppliersPageProps {
  searchParams: Promise<{ q?: string; page?: string; status?: string }>;
}

export async function SuppliersPage({ searchParams }: SuppliersPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Suppliers.View);
  if (denied) return denied;

  const canManage = hasPermission(session, PURCHASING_PERMISSIONS.Suppliers.Manage);

  const { q, page, status } = await searchParams;
  const pageNumber = parsePageNumber(page);
  const statusFilter = parseEnumValue(SupplierStatus, status);
  const searchValue = q?.trim() || undefined;

  const result = await searchSuppliers({
    status: statusFilter,
    searchValue,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load suppliers", description: result.message || "Please try again." }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Suppliers</h1>
        <p className="text-sm text-muted-foreground">
          The vendors you buy stock from. Inactive suppliers cannot be used on new purchase orders.
        </p>
      </div>

      <Card>
        <CardContent>
          <SuppliersDataTable
            status={statusFilter ?? ""}
            searchValue={searchValue ?? ""}
            records={paged?.records ?? []}
            pageNumber={paged?.pageNumber ?? pageNumber}
            pageSize={paged?.pageSize ?? PAGE_SIZE}
            totalPages={paged?.totalPages ?? 1}
            totalRecords={paged?.totalRecords ?? 0}
            error={error}
            canManage={canManage}
          />
        </CardContent>
      </Card>
    </div>
  );
}
