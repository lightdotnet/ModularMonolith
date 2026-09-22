import { Card, CardContent } from "@/components/ui/card";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import {
  PURCHASING_PERMISSIONS,
  PurchaseReturnStatus,
  LOCATION_HINT,
  LookupHints,
  SUPPLIER_ACCESS_HINT,
  SUPPLIER_FAILED_HINT,
  supplierTruncatedHint,
  parseEnumValue,
  parseNumericIdParam,
  parsePageNumber,
} from "@/modules/purchasing/common";
import { searchPurchaseReturns } from "@/modules/purchasing/purchase-returns/api/purchase-returns.api";
import { PurchaseReturnsDataTable } from "@/modules/purchasing/purchase-returns/components/purchase-returns-data-table";
import { SUPPLIER_OPTIONS_LIMIT, getSupplierOptions } from "@/modules/purchasing/suppliers";

const PAGE_SIZE = 10;
const MAX_LOCATION_ID_LENGTH = 450;

interface PurchaseReturnsPageProps {
  searchParams: Promise<{
    q?: string;
    page?: string;
    status?: string;
    supplierId?: string;
    locationId?: string;
    goodsReceiptId?: string;
  }>;
}

export async function PurchaseReturnsPage({ searchParams }: PurchaseReturnsPageProps) {
  const { session, denied } = await requirePermission(PURCHASING_PERMISSIONS.Returns.View);
  if (denied) return denied;

  const canViewSuppliers = hasPermission(session, PURCHASING_PERMISSIONS.Suppliers.View);
  const canCreate = hasPermission(session, PURCHASING_PERMISSIONS.Returns.Create);
  const canViewReceipts = hasPermission(session, PURCHASING_PERMISSIONS.Receipts.View);

  const { q, page, status, supplierId, locationId, goodsReceiptId } = await searchParams;
  const pageNumber = parsePageNumber(page);
  const statusFilter = parseEnumValue(PurchaseReturnStatus, status);
  const supplierFilter = parseNumericIdParam(supplierId);
  const receiptFilter = parseNumericIdParam(goodsReceiptId);
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

  const result = await searchPurchaseReturns({
    status: statusFilter,
    supplierId: supplierFilter,
    goodsReceiptId: receiptFilter,
    locationId: locationFilter,
    searchValue,
    pageNumber,
    pageSize: PAGE_SIZE,
  });

  const error =
    !result.isSuccess || !result.data
      ? { title: "Unable to load purchase returns", description: result.message || "Please try again." }
      : undefined;
  const paged = result.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Purchase returns</h1>
        <p className="text-sm text-muted-foreground">
          Goods sent back to suppliers.
          Returns are created from a posted goods receipt.
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
          <PurchaseReturnsDataTable
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
            canCreate={canCreate}
            canStart={canCreate && canViewReceipts}
          />
        </CardContent>
      </Card>
    </div>
  );
}
