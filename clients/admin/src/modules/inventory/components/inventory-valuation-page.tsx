import { Card, CardContent } from "@/components/ui/card";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import { getStockValuation } from "@/modules/inventory/api/stock-levels.api";
import { StockValuationTable } from "@/modules/inventory/components/stock-valuation-table";
import { INVENTORY_STOCK_PERMISSIONS } from "@/modules/inventory/constants/permissions";

const PAGE_SIZE = 10;

interface InventoryValuationPageProps {
  searchParams: Promise<{
    productId?: string;
    locationId?: string;
    page?: string;
  }>;
}

export async function InventoryValuationPage({ searchParams }: InventoryValuationPageProps) {
  const { denied } = await requirePermission(INVENTORY_STOCK_PERMISSIONS.ViewCost);
  if (denied) return denied;

  const { productId: rawProductId, locationId: rawLocationId, page: rawPage } = await searchParams;

  // Harden URL params: productId is a bigint (digit string only), locationId is an opaque string id
  // (single non-empty value, max 450 chars like the backend validator), page is a positive integer.
  // Anything else (arrays, junk) => ignored / page 1.
  const productId = typeof rawProductId === "string" && /^\d+$/.test(rawProductId) ? rawProductId : undefined;
  const locationId =
    typeof rawLocationId === "string" && rawLocationId.length > 0 && rawLocationId.length <= 450
      ? rawLocationId
      : undefined;
  const parsedPage = typeof rawPage === "string" ? Math.floor(Number(rawPage)) : NaN;
  const pageNumber = Number.isFinite(parsedPage) && parsedPage >= 1 ? parsedPage : 1;

  const [locationTreeResult, valuationResult] = await Promise.all([
    getLocationTree(),
    getStockValuation({
      productId: productId || undefined,
      locationId: locationId || undefined,
      pageNumber,
      pageSize: PAGE_SIZE,
    }),
  ]);

  const locations = locationTreeResult.data ? flattenLocationTree(locationTreeResult.data) : [];

  const error =
    !valuationResult.isSuccess || !valuationResult.data
      ? {
          title: "Unable to load stock valuation",
          description: valuationResult.message || "Please try again.",
        }
      : undefined;
  const valuation = valuationResult.data;
  const pageSize = valuation?.pageSize || PAGE_SIZE;
  const totalRecords = valuation?.totalRecords ?? 0;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Stock Valuation</h1>
        <p className="text-sm text-muted-foreground">Stock on hand valued at moving-average cost.</p>
      </div>

      <Card>
        <CardContent>
          <StockValuationTable
            locations={locations}
            productId={productId ?? ""}
            locationId={locationId ?? ""}
            records={valuation?.lines ?? []}
            pageNumber={valuation?.pageNumber ?? pageNumber}
            pageSize={pageSize}
            totalPages={Math.max(Math.ceil(totalRecords / pageSize), 1)}
            totalRecords={totalRecords}
            grandTotalQuantity={valuation?.grandTotalQuantity}
            grandTotalValue={valuation?.grandTotalValueBase}
            error={error}
          />
        </CardContent>
      </Card>
    </div>
  );
}
