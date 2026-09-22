import { Card, CardContent } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import { getOrderTypes } from "@/modules/orders/api/order-types.api";
import { searchOrders } from "@/modules/orders/api/orders.api";
import { OrdersDataTable } from "@/modules/orders/components/orders-data-table";
import { OrderTypesPanel } from "@/modules/orders/components/order-types-panel";
import {
  ORDERS_ORDER_TYPES_PERMISSIONS,
  ORDERS_PERMISSIONS,
} from "@/modules/orders/constants/permissions";
import { OrderStatus } from "@/modules/orders/types/order";
import { OrderTypeCategory } from "@/modules/orders/types/order-type";

const PAGE_SIZE = 10;

interface OrdersPageProps {
  searchParams: Promise<{ q?: string; page?: string; locationId?: string; status?: string }>;
}

export async function OrdersPage({ searchParams }: OrdersPageProps) {
  const { session, denied } = await requirePermission(ORDERS_PERMISSIONS.View);
  if (denied) return denied;

  const canManage = hasPermission(session, ORDERS_PERMISSIONS.Manage);
  const canViewPayments = hasPermission(session, ORDERS_PERMISSIONS.Payments.View);
  const canManagePayments = hasPermission(session, ORDERS_PERMISSIONS.Payments.Manage);
  const canViewOrderTypes = hasPermission(session, ORDERS_ORDER_TYPES_PERMISSIONS.View);
  const canManageOrderTypes = hasPermission(session, ORDERS_ORDER_TYPES_PERMISSIONS.Manage);

  const { q, page, locationId, status } = await searchParams;
  const pageNumber = Math.max(Number(page) || 1, 1);

  const [locationTreeResult, ordersResult, orderTypesResult] = await Promise.all([
    getLocationTree(),
    searchOrders({
      locationId: locationId || undefined,
      status: (status as OrderStatus) || undefined,
      searchValue: q,
      pageNumber,
      pageSize: PAGE_SIZE,
    }),
    getOrderTypes(),
  ]);

  const locations = locationTreeResult.data ? flattenLocationTree(locationTreeResult.data) : [];
  const orderTypes = orderTypesResult.data ?? [];
  const feeTypes = orderTypes.filter((t) => t.category === OrderTypeCategory.Fee);
  const paymentTypes = orderTypes.filter((t) => t.category === OrderTypeCategory.Payment);

  const error =
    !ordersResult.isSuccess || !ordersResult.data
      ? {
          title: "Unable to load orders",
          description: ordersResult.message || "Please try again.",
        }
      : undefined;
  const paged = ordersResult.data;

  const typesError = orderTypesResult.isSuccess ? undefined : orderTypesResult.message;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Orders</h1>
        <p className="text-sm text-muted-foreground">Build, place and cancel orders.</p>
      </div>

      <Tabs defaultValue="orders">
        <TabsList>
          <TabsTrigger value="orders">Orders</TabsTrigger>
          {canViewOrderTypes && <TabsTrigger value="types">Types</TabsTrigger>}
        </TabsList>

        <TabsContent value="orders">
          <Card>
            <CardContent>
              <OrdersDataTable
                locations={locations}
                feeTypes={feeTypes}
                paymentTypes={paymentTypes}
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
                canViewPayments={canViewPayments}
                canManagePayments={canManagePayments}
              />
            </CardContent>
          </Card>
        </TabsContent>

        {canViewOrderTypes && (
          <TabsContent value="types">
            <Card>
              <CardContent>
                <OrderTypesPanel
                  orderTypes={orderTypes}
                  error={typesError}
                  canManage={canManageOrderTypes}
                />
              </CardContent>
            </Card>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}
