import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import {
  GoodsReceiptStatus,
  PURCHASING_PERMISSIONS,
  LOCATION_HINT,
  LookupHints,
  SUPPLIER_ACCESS_HINT,
  SUPPLIER_FAILED_HINT,
  supplierTruncatedHint,
  parseEnumValue,
  parseNumericIdParam,
  parsePageNumber,
} from "@/modules/purchasing/common";
import { searchGoodsReceipts } from "@/modules/purchasing/goods-receipts/api/goods-receipts.api";
import { GoodsReceiptsDataTable } from "@/modules/purchasing/goods-receipts/components/goods-receipts-data-table";
import { SUPPLIER_OPTIONS_LIMIT, getSupplierOptions } from "@/modules/purchasing/suppliers";

const PAGE_SIZE = 10;
const MAX_LOCATION_ID_LENGTH = 450;

interface GoodsReceiptsPageProps {
  searchParams: Promise<{
    q?: string;
    page?: string;
    status?: string;
    supplierId?: string;
    locationId?: string;
    purchaseOrderId?: string;
  }>;
}

export async function GoodsReceiptsPage({ searchParams }: GoodsReceiptsPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Receipts.View);
  if (denied) return denied;

  // Returning needs returns.create to make the draft and returns.view to open it afterwards.
  const canReturn =
    hasPermission(session, PURCHASING_PERMISSIONS.Returns.Create) &&
    hasPermission(session, PURCHASING_PERMISSIONS.Returns.View);
  const canViewSuppliers = hasPermission(session, PURCHASING_PERMISSIONS.Suppliers.View);

  const { q, page, status, supplierId, locationId, purchaseOrderId } = await searchParams;
  const pageNumber = parsePageNumber(page);
  const statusFilter = parseEnumValue(GoodsReceiptStatus, status);
  const supplierFilter = parseNumericIdParam(supplierId);
  const purchaseOrderFilter = parseNumericIdParam(purchaseOrderId);
  const searchValue = q?.trim() || undefined;

  const [locationTreeResult, supplierLookup] = await Promise.all([
    getLocationTree(),
    canViewSuppliers ? getSupplierOptions(false) : null,
  ]);
  const locations = locationTreeResult.data ? flattenLocationTree(locationTreeResult.data) : [];
  const suppliers = (supplierLookup?.options ?? []).map((supplier) => ({
    value: supplier.id,
    label: supplier.name,
  }));

  const knownLocationIds = new Set(locations.map((location) => String(location.id)));
  const locationFilter =
    locationId && locationId.length <= MAX_LOCATION_ID_LENGTH && knownLocationIds.has(locationId)
      ? locationId
      : undefined;

  const result = await searchGoodsReceipts({
    status: statusFilter,
    supplierId: supplierFilter,
    locationId: locationFilter,
    purchaseOrderId: purchaseOrderFilter,
    searchValue,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load goods receipts", description: result.message || "Please try again." }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Goods receipts</h1>
        <p className="text-sm text-muted-foreground">
          Deliveries recorded against purchase orders. Receipts are immutable; return goods to the supplier
          from a posted receipt.
        </p>
      </div>

      <LookupHints
        hints={[
          !canViewSuppliers && SUPPLIER_ACCESS_HINT,
          supplierLookup?.failed && SUPPLIER_FAILED_HINT,
          supplierLookup?.truncated && supplierTruncatedHint(SUPPLIER_OPTIONS_LIMIT),
          !locationTreeResult.isSuccess && LOCATION_HINT,
        ]}
      />

      <Card>
        <CardContent>
          <GoodsReceiptsDataTable
            locations={locations}
            suppliers={suppliers}
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
            canReturn={canReturn}
          />
        </CardContent>
      </Card>
    </div>
  );
}
