import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import {
  PURCHASING_PERMISSIONS,
  PurchaseOrderStatus,
  LOCATION_HINT,
  LookupHints,
  SUPPLIER_ACCESS_HINT,
  SUPPLIER_FAILED_HINT,
  supplierTruncatedHint,
  parseEnumValue,
  parseNumericIdParam,
  parsePageNumber,
} from "@/modules/purchasing/common";
import { searchPurchaseOrders } from "@/modules/purchasing/purchase-orders/api/purchase-orders.api";
import { PurchaseOrdersDataTable } from "@/modules/purchasing/purchase-orders/components/purchase-orders-data-table";
import { SUPPLIER_OPTIONS_LIMIT, getSupplierOptions } from "@/modules/purchasing/suppliers";

const PAGE_SIZE = 10;
const MAX_LOCATION_ID_LENGTH = 450;

interface PurchaseOrdersPageProps {
  searchParams: Promise<{
    q?: string;
    page?: string;
    status?: string;
    supplierId?: string;
    locationId?: string;
  }>;
}

export async function PurchaseOrdersPage({ searchParams }: PurchaseOrdersPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Orders.View);
  if (denied) return denied;

  const canCreate = hasPermission(session, PURCHASING_PERMISSIONS.Orders.Create);
  // Suppliers are only needed for the create dialog and the filter; a viewer without the supplier
  // permission simply gets no supplier options (the backend would refuse the lookup anyway).
  const canViewSuppliers = hasPermission(session, PURCHASING_PERMISSIONS.Suppliers.View);

  const { q, page, status, supplierId, locationId } = await searchParams;
  const pageNumber = parsePageNumber(page);
  const statusFilter = parseEnumValue(PurchaseOrderStatus, status);
  const supplierFilter = parseNumericIdParam(supplierId);
  const searchValue = q?.trim() || undefined;

  const [locationTreeResult, allSuppliers, activeSuppliers] = await Promise.all([
    getLocationTree(),
    // The filter lists every supplier; the create dialog only offers Active ones (requested from the backend, so
    // a large supplier base cannot push an active supplier out of the list).
    canViewSuppliers ? getSupplierOptions(false) : null,
    canViewSuppliers && canCreate ? getSupplierOptions(true) : null,
  ]);
  const locations = locationTreeResult.data ? flattenLocationTree(locationTreeResult.data) : [];

  // Location ids are opaque strings — only accept ones that exist in the loaded location list.
  const knownLocationIds = new Set(locations.map((location) => String(location.id)));
  const locationFilter =
    locationId && locationId.length <= MAX_LOCATION_ID_LENGTH && knownLocationIds.has(locationId)
      ? locationId
      : undefined;

  const result = await searchPurchaseOrders({
    status: statusFilter,
    supplierId: supplierFilter,
    locationId: locationFilter,
    searchValue,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !result.isSuccess || !result.data
      ? {
          title: "Unable to load purchase orders",
          description: result.message || "Please try again.",
        }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Purchase orders</h1>
        <p className="text-sm text-muted-foreground">
          Order stock from suppliers: build the order, get it approved, then record what arrives.
        </p>
      </div>

      <LookupHints
        hints={[
          !canViewSuppliers && SUPPLIER_ACCESS_HINT,
          (allSuppliers?.failed || activeSuppliers?.failed) && SUPPLIER_FAILED_HINT,
          (allSuppliers?.truncated || activeSuppliers?.truncated) && supplierTruncatedHint(SUPPLIER_OPTIONS_LIMIT),
          !locationTreeResult.isSuccess && LOCATION_HINT,
        ]}
      />

      <Card>
        <CardContent>
          <PurchaseOrdersDataTable
            locations={locations}
            suppliers={allSuppliers?.options ?? []}
            activeSuppliers={activeSuppliers?.options ?? []}
            status={statusFilter ?? ""}
            supplierId={supplierFilter ?? ""}
            locationId={locationFilter ?? ""}
            searchValue={searchValue ?? ""}
            records={paged?.records ?? []}
            pageNumber={paged?.pageNumber ?? pageNumber}
            pageSize={paged?.pageSize ?? PAGE_SIZE}
            totalPages={paged?.totalPages ?? 1}
            totalRecords={paged?.totalRecords ?? 0}
            error={error}
            canCreate={canCreate}
          />
        </CardContent>
      </Card>
    </div>
  );
}
