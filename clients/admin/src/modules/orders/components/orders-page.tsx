import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import { searchOrders } from "@/modules/orders/api/orders.api";
import { OrdersDataTable } from "@/modules/orders/components/orders-data-table";
import { ORDERS_PERMISSIONS } from "@/modules/orders/constants/permissions";
import { OrderStatus } from "@/modules/orders/types/order";

const PAGE_SIZE = 10;

interface OrdersPageProps {
  searchParams: Promise<{ q?: string; page?: string; locationId?: string; status?: string }>;
}

export async function OrdersPage({ searchParams }: OrdersPageProps) {
  const { session, denied } = await requirePermission(ORDERS_PERMISSIONS.View);
  if (denied) return denied;

  const canManage = hasPermission(session, ORDERS_PERMISSIONS.Manage);

  const { q, page, locationId, status } = await searchParams;
  const pageNumber = Math.max(Number(page) || 1, 1);

  const [locationTreeResult, ordersResult] = await Promise.all([
    getLocationTree(),
    searchOrders({
      locationId: locationId || undefined,
      status: (status as OrderStatus) || undefined,
      searchValue: q,
      pageNumber,
      pageSize: PAGE_SIZE,
    }),
  ]);

  const locations = locationTreeResult.data ? flattenLocationTree(locationTreeResult.data) : [];

  const error =
    !ordersResult.isSuccess || !ordersResult.data
      ? {
          title: "Unable to load orders",
          description: ordersResult.message || "Please try again.",
        }
      : undefined;
  const paged = ordersResult.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Orders</h1>
        <p className="text-sm text-muted-foreground">Build, place and cancel orders.</p>
      </div>

      <Card>
        <CardContent>
          <OrdersDataTable
            locations={locations}
            locationId={locationId ?? ""}
            status={status ?? ""}
            records={paged?.records ?? []}
            searchValue={q ?? ""}
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
