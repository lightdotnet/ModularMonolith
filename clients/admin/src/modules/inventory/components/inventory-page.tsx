import { Card, CardContent } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getLocationTree } from "@/modules/location/api/locations.api";
import { flattenLocationTree } from "@/modules/location/types/location";
import { searchStockAdjustments } from "@/modules/inventory/api/stock-adjustments.api";
import { getProductStockTotal, searchStockLevels } from "@/modules/inventory/api/stock-levels.api";
import { StockAdjustmentsDataTable } from "@/modules/inventory/components/stock-adjustments-data-table";
import { StockLevelsDataTable } from "@/modules/inventory/components/stock-levels-data-table";
import { INVENTORY_STOCK_PERMISSIONS } from "@/modules/inventory/constants/permissions";

const PAGE_SIZE = 10;

interface InventoryPageProps {
  searchParams: Promise<{
    levelProductId?: string;
    levelLocationId?: string;
    levelPage?: string;
    adjProductId?: string;
    adjLocationId?: string;
    adjSourceOrderId?: string;
    adjPage?: string;
  }>;
}

export async function InventoryPage({ searchParams }: InventoryPageProps) {
  const { session, denied } = await requirePermission(INVENTORY_STOCK_PERMISSIONS.View);
  if (denied) return denied;

  const canManage = hasPermission(session, INVENTORY_STOCK_PERMISSIONS.Manage);

  const {
    levelProductId,
    levelLocationId,
    levelPage,
    adjProductId,
    adjLocationId,
    adjSourceOrderId,
    adjPage,
  } = await searchParams;

  const levelPageNumber = Math.max(Number(levelPage) || 1, 1);
  const adjPageNumber = Math.max(Number(adjPage) || 1, 1);

  const [locationTreeResult, stockLevelsResult, stockAdjustmentsResult, productTotalResult] =
    await Promise.all([
      getLocationTree(),
      searchStockLevels({
        productId: levelProductId || undefined,
        locationId: levelLocationId || undefined,
        pageNumber: levelPageNumber,
        pageSize: PAGE_SIZE,
      }),
      searchStockAdjustments({
        productId: adjProductId || undefined,
        locationId: adjLocationId || undefined,
        sourceOrderId: adjSourceOrderId || undefined,
        pageNumber: adjPageNumber,
        pageSize: PAGE_SIZE,
      }),
      levelProductId ? getProductStockTotal(levelProductId) : Promise.resolve(undefined),
    ]);

  const locations = locationTreeResult.data ? flattenLocationTree(locationTreeResult.data) : [];

  const levelsError =
    !stockLevelsResult.isSuccess || !stockLevelsResult.data
      ? {
          title: "Unable to load stock levels",
          description: stockLevelsResult.message || "Please try again.",
        }
      : undefined;
  const levelsPaged = stockLevelsResult.data;
  const productTotal = productTotalResult?.data ?? undefined;

  const adjustmentsError =
    !stockAdjustmentsResult.isSuccess || !stockAdjustmentsResult.data
      ? {
          title: "Unable to load stock adjustments",
          description: stockAdjustmentsResult.message || "Please try again.",
        }
      : undefined;
  const adjustmentsPaged = stockAdjustmentsResult.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Inventory</h1>
        <p className="text-sm text-muted-foreground">Review stock on hand and the stock adjustment history.</p>
      </div>

      <Tabs defaultValue="levels">
        <TabsList>
          <TabsTrigger value="levels">Stock Levels</TabsTrigger>
          <TabsTrigger value="adjustments">Adjustments</TabsTrigger>
        </TabsList>

        <TabsContent value="levels">
          <Card>
            <CardContent>
              <StockLevelsDataTable
                locations={locations}
                productId={levelProductId ?? ""}
                locationId={levelLocationId ?? ""}
                records={levelsPaged?.records ?? []}
                pageNumber={levelsPaged?.pageNumber ?? levelPageNumber}
                pageSize={levelsPaged?.pageSize ?? PAGE_SIZE}
                totalPages={levelsPaged?.totalPages ?? 1}
                totalRecords={levelsPaged?.totalRecords ?? 0}
                error={levelsError}
                productTotal={productTotal}
              />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="adjustments">
          <Card>
            <CardContent>
              <StockAdjustmentsDataTable
                locations={locations}
                productId={adjProductId ?? ""}
                locationId={adjLocationId ?? ""}
                sourceOrderId={adjSourceOrderId ?? ""}
                records={adjustmentsPaged?.records ?? []}
                pageNumber={adjustmentsPaged?.pageNumber ?? adjPageNumber}
                pageSize={adjustmentsPaged?.pageSize ?? PAGE_SIZE}
                totalPages={adjustmentsPaged?.totalPages ?? 1}
                totalRecords={adjustmentsPaged?.totalRecords ?? 0}
                error={adjustmentsError}
                canManage={canManage}
              />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
